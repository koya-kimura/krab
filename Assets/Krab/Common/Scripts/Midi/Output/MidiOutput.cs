using System;
using System.Collections.Concurrent;
using System.Threading;
using RtMidi;
using UnityEngine;

namespace Krab.Common.Midi.Output
{
    /// <summary>Owns the RtMidi output port. Native calls stay on the worker thread.</summary>
    public sealed class MidiOutput : MonoBehaviour, IMidiOutput
    {
        [SerializeField] private string _portNameFilter = "APC mini mk2";
        [SerializeField, Min(0.5f)] private float _reconnectInterval = 2f;

        private readonly ConcurrentQueue<byte[]> _queue = new();
        private Thread _worker;
        private WorkerState _workerState;
        private volatile bool _connected;
        private volatile string _portName;
        private volatile string _lastError;
        private bool _lastPublishedConnection;

        public bool IsConnected => _connected;
        public string PortName => _portName;
        public string LastError => _lastError;

        /// <summary>Raised on the Unity main thread after an output port connects or disconnects.</summary>
        public event Action<bool> ConnectionChanged;

        private void OnEnable()
        {
            StartWorkerIfStopped();
        }

        private void Update()
        {
            StartWorkerIfStopped();
            if (_lastPublishedConnection == _connected) return;

            _lastPublishedConnection = _connected;
            ConnectionChanged?.Invoke(_connected);
        }

        private void OnDisable()
        {
            _workerState?.Stop();
            // A native driver may block; never stall the Unity main thread indefinitely.
            _worker?.Join(100);
            while (_queue.TryDequeue(out _)) { }
        }

        private void OnDestroy()
        {
            _workerState?.Stop();
        }

        private void StartWorkerIfStopped()
        {
            if (_worker != null && _worker.IsAlive) return;

            var state = new WorkerState();
            _workerState = state;
            _worker = new Thread(() => WorkerLoop(state)) { IsBackground = true, Name = "Krab MIDI Output" };
            _worker.Start();
        }

        public void SendNoteOn(int noteNumber, int velocity, int channel = 0)
        {
            ValidateDataByte(noteNumber, nameof(noteNumber));
            ValidateDataByte(velocity, nameof(velocity));
            ValidateChannel(channel);

            Span<byte> message = stackalloc byte[3];
            message[0] = (byte)(0x90 | channel);
            message[1] = (byte)noteNumber;
            message[2] = (byte)velocity;
            SendRaw(message);
        }

        public void SendNoteOff(int noteNumber, int channel = 0)
        {
            ValidateDataByte(noteNumber, nameof(noteNumber));
            ValidateChannel(channel);

            Span<byte> message = stackalloc byte[3];
            message[0] = (byte)(0x80 | channel);
            message[1] = (byte)noteNumber;
            message[2] = 0;
            SendRaw(message);
        }

        public void SendCc(int number, int value, int channel = 0)
        {
            ValidateDataByte(number, nameof(number));
            ValidateDataByte(value, nameof(value));
            ValidateChannel(channel);

            Span<byte> message = stackalloc byte[3];
            message[0] = (byte)(0xb0 | channel);
            message[1] = (byte)number;
            message[2] = (byte)value;
            SendRaw(message);
        }

        private static void ValidateDataByte(int value, string name)
        {
            if (value is < 0 or > 127) throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidateChannel(int channel)
        {
            if (channel is < 0 or > 15) throw new ArgumentOutOfRangeException(nameof(channel));
        }

        public void SendRaw(ReadOnlySpan<byte> message)
        {
            var state = _workerState;
            if (message.IsEmpty || !_connected || state == null || state.IsStopped) return;

            _queue.Enqueue(message.ToArray());
            // When the driver is slow, recent LED state wins over stale frames.
            while (_queue.Count > 512) _queue.TryDequeue(out _);
            state.Wake();
        }

        private void WorkerLoop(WorkerState state)
        {
            MidiOut port = null;
            try
            {
                while (!state.IsStopped)
                {
                    if (port == null)
                    {
                        try
                        {
                            port = OpenPort();
                            if (port != null)
                            {
                                _connected = true;
                                _lastError = null;
                            }
                        }
                        catch (Exception error)
                        {
                            _lastError = error.Message;
                            port?.Dispose();
                            port = null;
                        }

                        if (port == null)
                        {
                            state.Wait(Mathf.RoundToInt(Mathf.Max(0.5f, _reconnectInterval) * 1000));
                            continue;
                        }
                    }

                    while (!state.IsStopped && _queue.TryDequeue(out var bytes))
                    {
                        try
                        {
                            if (port.SendMessage(bytes) >= 0) continue;

                            _lastError = port.IsOk ? "MIDI send failed" : port.Error;
                        }
                        catch (Exception error)
                        {
                            _lastError = error.Message;
                        }

                        _connected = false;
                        _portName = null;
                        port.Dispose();
                        port = null;
                        while (_queue.TryDequeue(out _)) { }
                        break;
                    }

                    state.Wait(50);
                }
            }
            finally
            {
                _connected = false;
                _portName = null;
                port?.Dispose();
                state.Dispose();
            }
        }

        private sealed class WorkerState : IDisposable
        {
            private readonly AutoResetEvent _wake = new(false);
            private readonly object _sync = new();
            private volatile bool _stopped;
            private bool _disposed;

            public bool IsStopped => _stopped;
            public void Wake()
            {
                lock (_sync) if (!_disposed) _wake.Set();
            }
            public void Stop()
            {
                _stopped = true;
                Wake();
            }
            public void Wait(int milliseconds) => _wake.WaitOne(milliseconds);
            public void Dispose()
            {
                lock (_sync)
                {
                    if (_disposed) return;
                    _disposed = true;
                    _wake.Dispose();
                }
            }
        }

        private MidiOut OpenPort()
        {
            var port = MidiOut.Create(Api.Unspecified, "Krab");
            try
            {
                if (port.IsInvalid)
                {
                    port.Dispose();
                    _lastError = "Could not create a MIDI output port.";
                    return null;
                }

                var filter = _portNameFilter ?? string.Empty;
                for (var index = 0; index < port.PortCount; index++)
                {
                    var name = port.GetPortName(index);
                    if (string.IsNullOrEmpty(name) ||
                        name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    port.OpenPort(index, "Krab MIDI Output");
                    if (port.IsOk)
                    {
                        _portName = name;
                        return port;
                    }

                    _lastError = port.Error;
                    port.Dispose();
                    return null;
                }

                _lastError = $"MIDI output port not found: {filter}";
                port.Dispose();
                return null;
            }
            catch
            {
                port.Dispose();
                throw;
            }
        }
    }
}
