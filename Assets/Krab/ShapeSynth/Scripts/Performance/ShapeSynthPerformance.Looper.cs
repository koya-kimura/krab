using System;
using System.Collections.Generic;
using Krab.Common.Looping.Core;
using Krab.Common.Looping.Midi;
using Krab.Common.Looping.Views;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        public const int LooperCount = 8;

        private const string LooperActionPrefix = "ShapeSynth.Looper";

        private readonly List<LoopPlaybackEvent<string>> _loopEvents = new();
        private readonly LooperBank<string> _loopers = new(LooperCount, () => LooperClock);
        private LooperBankMidi _looperMidi;
        private bool _loopersStarted;
        private string _looperError;

        private void OnEnable()
        {
            if (_loopersStarted) AttachLooperMidi();
        }

        private void OnDisable()
        {
            if (!_loopersStarted) return;

            _loopers.ClearAll();
            DetachLooperMidi();
        }

        public LooperBank<string> Loopers => _loopers;

        public string LooperError => _looperError;

        // Like the p5 looper, loops keep their recorded length in real seconds and ignore BPM changes.
        private static double LooperClock => Time.unscaledTimeAsDouble;

        public void AdvanceLooper(int index)
        {
            if (!isActiveAndEnabled) return;

            _loopers.Cycle(index);
        }

        public void ClearLooper(int index)
        {
            _loopers.Clear(index);
        }

        private void InitializeLoopers()
        {
            _loopersStarted = true;
            _loopers.StateChanged += OnLooperStateChanged;
            AttachLooperMidi();
        }

        private void OnLooperStateChanged(int index)
        {
            if (_loopers.GetState(index) == EventLooperState.Recording) _looperError = null;
        }

        private void RecordManualTrigger(string presetId)
        {
            if (!_loopersStarted || !isActiveAndEnabled) return;

            _loopers.Record(presetId);
        }

        private void UpdateLoopers()
        {
            if (!_loopersStarted) return;

            for (var index = 0; index < LooperCount; index++)
            {
                var dropped = _loopers[index].DroppedEventCount;
                _loopEvents.Clear();
                _loopers.Advance(index, _loopEvents);
                if (_loopers[index].DroppedEventCount != dropped)
                    _looperError = $"Loop {index + 1}: skipped overdue events after a long frame.";

                foreach (var loopEvent in _loopEvents)
                {
                    // Playback spawns without recording, so a replay never feeds another recording slot.
                    if (SpawnUserPreset(loopEvent.Payload) != null) continue;

                    // A missing or invalid preset stops only the affected loop, not the other performers.
                    _looperError = $"Loop {index + 1} stopped: {_error}";
                    _loopers.Clear(index);
                    break;
                }
            }
        }

        private void AttachLooperMidi()
        {
            if (_midiBindings == null || _looperMidi != null) return;

            _looperMidi = new LooperBankMidi(_midiBindings, _loopers, LooperActionPrefix);
        }

        private void DetachLooperMidi()
        {
            _looperMidi?.Dispose();
            _looperMidi = null;
        }

        private Element BuildLoopers()
        {
            Func<int, Element> cycleMidi = null;
            Func<int, Element> clearMidi = null;
            if (_midiUi != null)
            {
                cycleMidi = slot => _midiUi.CreateButton(
                    LooperBankMidi.CycleActionId(LooperActionPrefix, slot), $"Loop {slot + 1}");
                clearMidi = slot => _midiUi.CreateButton(
                    LooperBankMidi.ClearActionId(LooperActionPrefix, slot), $"Clear Loop {slot + 1}");
            }

            return UI.Column(
                LooperBankUi.Create(_loopers, "s", cycleMidi, clearMidi),
                UI.DynamicElementIf(() => _looperError != null,
                    () => UI.Label(() => _looperError).SetColor(ErrorColor)));
        }
    }
}
