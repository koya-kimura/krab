using System;
using System.Collections.Generic;
using Krab.Common.Midi.Core;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Devices.ApcMiniMk2;
using Krab.Common.Midi.Output;
using Krab.Common.Timing;
using Minis;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Krab.Common.Midi.Input
{
    [RequireComponent(typeof(MidiOutput))]
    public class MidiInputManager : MonoBehaviour, IMidiBindingProvider, IApcMiniMk2Surface
    {
        private const float LedFlushInterval = 1f / 15f;
        private const int MaxPadMessagesPerFlush = 32;

        private readonly HashSet<MidiDevice> _subscribedDevices = new();
        private readonly Color[] _sentPadColors = new Color[ApcMiniMk2.GridSize * ApcMiniMk2.GridSize];
        private readonly bool[] _sentTrackLeds = new bool[ApcMiniMk2.GridSize];
        private readonly Color[] _desiredPadColors = new Color[ApcMiniMk2.GridSize * ApcMiniMk2.GridSize];
        private readonly bool[] _desiredTrackLeds = new bool[ApcMiniMk2.GridSize];
        private MidiBindingManager _bindings;
        private ApcMiniMk2InputAdapter _adapter;
        private ApcMiniMk2FaderModeController _faderModes;
        private MidiOutput _output;
        private ApcMiniMk2OutputAdapter _ledOutput;
        private IBPMManager _bpm;
        private Color _desiredShiftColor;
        private bool _refreshAllLeds = true;
        private float _nextLedFlushTime;

        public MidiBindingManager Bindings => _bindings ??= new MidiBindingManager();
        public ApcMiniMk2FaderModeController FaderModes => _faderModes ??= new ApcMiniMk2FaderModeController();
        public ApcMiniMk2InputAdapter Adapter => _adapter ??= new ApcMiniMk2InputAdapter(Bindings, FaderModes);
        public Color GetPadColor(int note) => note >= ApcMiniMk2.PadFirst && note <= ApcMiniMk2.PadLast
            ? _desiredPadColors[note - ApcMiniMk2.PadFirst]
            : Color.black;
        public bool GetTrackLed(int index) => index >= 0 && index < _desiredTrackLeds.Length && _desiredTrackLeds[index];
        public Color GetShiftColor() => _desiredShiftColor;

        [UnitySimpleContainer.Inject]
        public void Construct(IBPMManager bpm)
        {
            _bpm = bpm;
        }

        private void Awake()
        {
            _output = GetComponent<MidiOutput>();
            _ledOutput = new ApcMiniMk2OutputAdapter(_output);
            Bindings.BindingChanged += OnBindingChanged;
            Adapter.PageChanged += OnPageChanged;
        }

        private void OnEnable()
        {
            _refreshAllLeds = true;
            _output.ConnectionChanged += OnConnectionChanged;
            InputSystem.onDeviceChange += OnDeviceChange;
            foreach (var device in InputSystem.devices)
            {
                if (device is MidiDevice midi) Subscribe(midi);
            }
        }

        private void OnDisable()
        {
            _output.ConnectionChanged -= OnConnectionChanged;
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var midi in _subscribedDevices) UnsubscribeCallbacks(midi);
            _subscribedDevices.Clear();
            _bindings?.ReleaseTransientActions();
        }

        private void OnDestroy()
        {
            if (_bindings != null) _bindings.BindingChanged -= OnBindingChanged;
            if (_adapter != null) _adapter.PageChanged -= OnPageChanged;
            _adapter?.Dispose();
            _bindings?.Dispose();
        }

        private void Update()
        {
            var beatIndex = _bpm == null ? 0L : (long)Math.Floor(_bpm.Beat);
            Bindings.UpdateActions(Time.frameCount, beatIndex);
            FaderModes.SetBeat(beatIndex);
            UpdateDesiredLeds();
            FlushLeds();
        }

        private void OnConnectionChanged(bool connected)
        {
            if (connected) _refreshAllLeds = true;
        }

        private void OnPageChanged(int page)
        {
            _refreshAllLeds = true;
        }

        private void OnBindingChanged(string paramId, MidiControl? control)
        {
            _refreshAllLeds = true;
        }

        private void UpdateDesiredLeds()
        {
            for (var i = 0; i < _desiredPadColors.Length; i++) _desiredPadColors[i] = Color.black;
            for (var i = 0; i < _desiredTrackLeds.Length; i++)
                _desiredTrackLeds[i] = FaderModes.IsAssignedModeActive(i);
            _desiredShiftColor = Color.black;
            foreach (var binding in Bindings.Bindings)
            {
                if (binding.Control is not { } control || control.Type != MidiMessageType.Note) continue;
                if (control.Number == ApcMiniMk2.ShiftButton && control.Page == ApcMiniMk2.GlobalPage)
                {
                    _desiredShiftColor = binding.ResolveLedColor();
                    continue;
                }
                if (control.Page != Adapter.CurrentPage) continue;
                if (control.Number >= ApcMiniMk2.PadFirst && control.Number <= ApcMiniMk2.PadLast)
                {
                    _desiredPadColors[control.Number - ApcMiniMk2.PadFirst] = binding.ResolveLedColor();
                }
            }
        }

        private void FlushLeds()
        {
            if (!_output.IsConnected || Time.unscaledTime < _nextLedFlushTime) return;
            _nextLedFlushTime = Time.unscaledTime + LedFlushInterval;

            if (_refreshAllLeds)
            {
                // Clear the previous page before painting the selected page.
                _ledOutput.SetPadRange(ApcMiniMk2.PadFirst, ApcMiniMk2.PadLast, Color.black);
                for (var i = 0; i < _sentPadColors.Length; i++) _sentPadColors[i] = Color.black;
                for (var i = 0; i < ApcMiniMk2.GridSize; i++)
                {
                    _ledOutput.SetButton(ApcMiniMk2.TrackButtonFirst + i, false);
                    _ledOutput.SetButton(ApcMiniMk2.PageButtonFirst + i, i == Adapter.CurrentPage);
                    _sentTrackLeds[i] = false;
                }
                _refreshAllLeds = false;
            }

            var sent = 0;
            for (var i = 0; i < _desiredPadColors.Length && sent < MaxPadMessagesPerFlush; i++)
            {
                if (_sentPadColors[i] == _desiredPadColors[i]) continue;

                _ledOutput.SetPad(ApcMiniMk2.PadFirst + i, _desiredPadColors[i]);
                _sentPadColors[i] = _desiredPadColors[i];
                sent++;
            }
            for (var i = 0; i < _desiredTrackLeds.Length; i++)
            {
                if (_sentTrackLeds[i] == _desiredTrackLeds[i]) continue;

                _ledOutput.SetButton(ApcMiniMk2.TrackButtonFirst + i, _desiredTrackLeds[i]);
                _sentTrackLeds[i] = _desiredTrackLeds[i];
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not MidiDevice midi) return;

            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
                Subscribe(midi);
            else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
                Unsubscribe(midi);
        }

        private void Subscribe(MidiDevice midi)
        {
            if (!_subscribedDevices.Add(midi)) return;

            midi.onWillNoteOn += OnNoteOn;
            midi.onWillNoteOff += OnNoteOff;
            midi.onWillControlChange += OnControlChange;
        }

        private void Unsubscribe(MidiDevice midi)
        {
            if (!_subscribedDevices.Remove(midi)) return;
            UnsubscribeCallbacks(midi);
            Bindings.ReleaseTransientActions();
        }

        private void UnsubscribeCallbacks(MidiDevice midi)
        {
            midi.onWillNoteOn -= OnNoteOn;
            midi.onWillNoteOff -= OnNoteOff;
            midi.onWillControlChange -= OnControlChange;
        }

        private void OnNoteOn(MidiNoteControl note, float velocity) => Adapter.OnNoteOn(note.noteNumber, velocity);
        private void OnNoteOff(MidiNoteControl note) => Adapter.OnNoteOff(note.noteNumber);
        private void OnControlChange(MidiValueControl cc, float value) =>
            Adapter.OnControlChange(cc.controlNumber, value);
    }
}
