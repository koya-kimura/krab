using System;
using System.Collections.Generic;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using Krab.Common.Midi.Devices.ApcMiniMk2;
using Krab.Common.Midi.Devices.Mpd218;
using Krab.Common.Midi.Output;
using Krab.Common.Timing;
using Minis;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitySimpleContainer;

namespace Krab.Common.Midi.Input
{
    [RequireComponent(typeof(MidiOutput))]
    public class MidiInputManager : MonoBehaviour, IMidiBindingProvider, IApcMiniMk2Surface,
        IMpd218Surface
    {
        private const float LedFlushInterval = 1f / 15f;
        private const int MaxPadMessagesPerFlush = 32;

        private readonly Dictionary<MidiDevice, MidiDeviceKind> _subscribedDevices = new();
        private readonly Color[] _desiredMpd218Colors = new Color[128];
        private readonly Color[] _sentPadColors = new Color[ApcMiniMk2.GridSize * ApcMiniMk2.GridSize];
        private readonly bool[] _sentTrackLeds = new bool[ApcMiniMk2.GridSize];
        private readonly Color[] _desiredPadColors = new Color[ApcMiniMk2.GridSize * ApcMiniMk2.GridSize];
        private readonly bool[] _desiredTrackLeds = new bool[ApcMiniMk2.GridSize];
        private MidiBindingManager _bindings;
        private ApcMiniMk2InputAdapter _adapter;
        private Mpd218InputAdapter _mpd218Adapter;
        private ApcMiniMk2FaderModeController _faderModes;
        private MidiOutput _output;
        private ApcMiniMk2OutputAdapter _ledOutput;
        private IBPMManager _bpm;
        private Color _desiredShiftColor;
        private bool _refreshAllLeds = true;
        private float _nextLedFlushTime;

        private void Awake()
        {
            _output = GetComponent<MidiOutput>();
            _ledOutput = new ApcMiniMk2OutputAdapter(_output);
            Bindings.BindingChanged += OnBindingChanged;
            Adapter.PageChanged += OnPageChanged;
        }

        private void Update()
        {
            var beatIndex = _bpm == null ? 0L : (long)Math.Floor(_bpm.Beat);
            Bindings.UpdateActions(Time.frameCount, beatIndex);
            FaderModes.SetBeat(beatIndex);
            UpdateDesiredLeds();
            FlushLeds();
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
            foreach (var midi in _subscribedDevices.Keys) UnsubscribeCallbacks(midi);

            _subscribedDevices.Clear();
            _mpd218Adapter?.ReleaseAllNotes();
            _bindings?.ReleaseTransientActions();
        }

        private void OnDestroy()
        {
            if (_bindings != null) _bindings.BindingChanged -= OnBindingChanged;
            if (_adapter != null) _adapter.PageChanged -= OnPageChanged;

            _adapter?.Dispose();
            _bindings?.Dispose();
        }

        public MidiBindingManager Bindings => _bindings ??= new MidiBindingManager();
        public ApcMiniMk2FaderModeController FaderModes => _faderModes ??= new ApcMiniMk2FaderModeController();
        public ApcMiniMk2InputAdapter Adapter => _adapter ??= new ApcMiniMk2InputAdapter(Bindings, FaderModes);
        public Mpd218InputAdapter Mpd218Adapter => _mpd218Adapter ??= new Mpd218InputAdapter(Bindings);
        Mpd218InputAdapter IMpd218Surface.Adapter => Mpd218Adapter;

        [Inject]
        public void Construct(IBPMManager bpm)
        {
            _bpm = bpm;
        }

        public Color GetPadColor(int note)
        {
            return note >= ApcMiniMk2.PadFirst && note <= ApcMiniMk2.PadLast
                ? _desiredPadColors[note - ApcMiniMk2.PadFirst]
                : Color.black;
        }

        public bool GetTrackLed(int index)
        {
            return index >= 0 && index < _desiredTrackLeds.Length && _desiredTrackLeds[index];
        }

        public Color GetShiftColor()
        {
            return _desiredShiftColor;
        }

        Color IMpd218Surface.GetPadColor(int note)
        {
            return note >= 0 && note < _desiredMpd218Colors.Length ? _desiredMpd218Colors[note] : Color.black;
        }

        private void OnConnectionChanged(bool connected)
        {
            if (connected) _refreshAllLeds = true;
        }

        private void OnPageChanged(int page)
        {
            _refreshAllLeds = true;
        }

        private void OnBindingChanged(string paramId)
        {
            _refreshAllLeds = true;
        }

        private void UpdateDesiredLeds()
        {
            for (var i = 0; i < _desiredPadColors.Length; i++) _desiredPadColors[i] = Color.black;
            for (var i = 0; i < _desiredTrackLeds.Length; i++)
                _desiredTrackLeds[i] = FaderModes.IsAssignedModeActive(i);

            for (var i = 0; i < _desiredMpd218Colors.Length; i++) _desiredMpd218Colors[i] = Color.black;

            _desiredShiftColor = Color.black;
            foreach (var binding in Bindings.Bindings)
            {
                // The MPD218 LEDs are not driven; these colors only reach its simulator.
                if (binding.GetControl(MidiDeviceKind.Mpd218) is { Type: MidiMessageType.Note } mpd218 &&
                    mpd218.Number >= 0 && mpd218.Number < _desiredMpd218Colors.Length)
                    _desiredMpd218Colors[mpd218.Number] = binding.ResolveLedColor();

                if (binding.GetControl(MidiDeviceKind.ApcMiniMk2) is not { Type: MidiMessageType.Note } control)
                    continue;

                if (control.Number == ApcMiniMk2.ShiftButton && control.Page == ApcMiniMk2.GlobalPage)
                {
                    _desiredShiftColor = binding.ResolveLedColor();
                    continue;
                }

                if (control.Page != Adapter.CurrentPage) continue;
                if (control.Number >= ApcMiniMk2.PadFirst && control.Number <= ApcMiniMk2.PadLast)
                    _desiredPadColors[control.Number - ApcMiniMk2.PadFirst] = binding.ResolveLedColor();
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
            if (_subscribedDevices.ContainsKey(midi) || !TryGetDeviceKind(midi, out var kind)) return;

            _subscribedDevices.Add(midi, kind);
            midi.onWillNoteOn += OnNoteOn;
            midi.onWillNoteOff += OnNoteOff;
            midi.onWillControlChange += OnControlChange;
        }

        private void Unsubscribe(MidiDevice midi)
        {
            if (!_subscribedDevices.Remove(midi, out var kind)) return;

            UnsubscribeCallbacks(midi);
            if (kind == MidiDeviceKind.Mpd218) Mpd218Adapter.ReleaseAllNotes();
            Bindings.ReleaseTransientActions();
        }

        private void UnsubscribeCallbacks(MidiDevice midi)
        {
            midi.onWillNoteOn -= OnNoteOn;
            midi.onWillNoteOff -= OnNoteOff;
            midi.onWillControlChange -= OnControlChange;
        }

        private void OnNoteOn(MidiNoteControl note, float velocity)
        {
            if (GetSubscribedKind(note.device) == MidiDeviceKind.Mpd218)
                Mpd218Adapter.OnNoteOn(note.noteNumber, velocity);
            else
                Adapter.OnNoteOn(note.noteNumber, velocity);
        }

        private void OnNoteOff(MidiNoteControl note)
        {
            if (GetSubscribedKind(note.device) == MidiDeviceKind.Mpd218)
                Mpd218Adapter.OnNoteOff(note.noteNumber);
            else
                Adapter.OnNoteOff(note.noteNumber);
        }

        private void OnControlChange(MidiValueControl cc, float value)
        {
            if (GetSubscribedKind(cc.device) == MidiDeviceKind.Mpd218)
                Mpd218Adapter.OnControlChange(cc.controlNumber, value);
            else
                Adapter.OnControlChange(cc.controlNumber, value);
        }

        // Callbacks only arrive from subscribed devices, so the lookup always succeeds.
        private MidiDeviceKind GetSubscribedKind(InputDevice device)
        {
            return _subscribedDevices[(MidiDevice)device];
        }

        // Other MIDI devices are ignored so their notes never reach APC or MPD218 bindings.
        private static bool TryGetDeviceKind(MidiDevice midi, out MidiDeviceKind kind)
        {
            var product = midi.description.product;
            if (Mpd218.IsProduct(product))
            {
                kind = MidiDeviceKind.Mpd218;
                return true;
            }

            kind = MidiDeviceKind.ApcMiniMk2;
            return ApcMiniMk2.IsProduct(product);
        }
    }
}
