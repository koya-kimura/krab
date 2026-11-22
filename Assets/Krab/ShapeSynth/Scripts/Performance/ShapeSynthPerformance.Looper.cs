using System;
using System.Collections.Generic;
using Krab.Common.Looping.Core;
using Krab.Common.Midi.Core;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        public const int LooperCount = 8;

        private readonly EventLooper<string>[] _loopers = new EventLooper<string>[LooperCount];
        private readonly Param<int>[] _looperStates = new Param<int>[LooperCount];
        private readonly Param<bool>[] _looperClear = new Param<bool>[LooperCount];
        private readonly Action<int>[] _looperStateHandlers = new Action<int>[LooperCount];
        private readonly List<LoopPlaybackEvent<string>> _loopEvents = new();
        private readonly List<string> _looperActionIds = new();
        private bool _loopersInitialized;
        private bool _syncingLooperState;
        private string _looperError;

        private static readonly Color[] LooperColors = { Color.gray, Color.red, Color.green };

        public IReadOnlyList<EventLooper<string>> Loopers => _loopers;

        public string LooperError => _looperError;

        // Like the p5 looper, loops keep their recorded length in real seconds and ignore BPM changes.
        private static double LooperClock => Time.unscaledTimeAsDouble;

        private void OnEnable()
        {
            if (_loopersInitialized) RegisterLooperMidi();
        }

        private void OnDisable()
        {
            if (!_loopersInitialized) return;

            for (var index = 0; index < LooperCount; index++) ClearLooper(index);
            UnregisterLooperMidi();
        }

        private void InitializeLoopers()
        {
            for (var index = 0; index < LooperCount; index++)
            {
                _loopers[index] = new EventLooper<string>();
                _looperStates[index] = new Param<int>(0);
                _looperClear[index] = new Param<bool>(false);
            }

            _loopersInitialized = true;
            for (var index = 0; index < LooperCount; index++)
            {
                var slot = index;
                _looperStateHandlers[index] = state => ChangeLooperState(slot, state);
                _looperStates[index].Changed += _looperStateHandlers[index];
                _looperClear[index].Changed += pressed =>
                {
                    if (pressed && isActiveAndEnabled) ClearLooper(slot);
                };
            }

            RegisterLooperMidi();
        }

        public void AdvanceLooper(int index)
        {
            RequireLooper(index);
            if (!isActiveAndEnabled) return;

            _looperStates[index].SetValue(((int)_loopers[index].State + 1) % 3);
        }

        public void ClearLooper(int index)
        {
            RequireLooper(index);
            _loopers[index].Clear();
            SyncLooperState(index);
        }

        private void ChangeLooperState(int index, int state)
        {
            if (_syncingLooperState || !isActiveAndEnabled) return;

            var looper = _loopers[index];
            switch (state)
            {
                case 0:
                    looper.Clear();
                    break;
                case 1:
                    looper.StartRecording(LooperClock);
                    _looperError = null;
                    break;
                case 2:
                    looper.FinishRecording(LooperClock);
                    break;
            }

            SyncLooperState(index);
        }

        private void SyncLooperState(int index)
        {
            _syncingLooperState = true;
            try
            {
                _looperStates[index].SetValue((int)_loopers[index].State);
            }
            finally
            {
                _syncingLooperState = false;
            }
        }

        private void RecordManualTrigger(string presetId)
        {
            if (!_loopersInitialized || !isActiveAndEnabled) return;

            foreach (var looper in _loopers)
            {
                if (looper.State == EventLooperState.Recording) looper.Record(presetId, LooperClock);
            }
        }

        private void UpdateLoopers()
        {
            if (!_loopersInitialized) return;

            for (var index = 0; index < LooperCount; index++)
            {
                var looper = _loopers[index];
                var dropped = looper.DroppedEventCount;
                _loopEvents.Clear();
                looper.Advance(LooperClock, _loopEvents);
                if (looper.DroppedEventCount != dropped)
                    _looperError = $"Loop {index + 1}: skipped overdue events after a long frame.";

                foreach (var loopEvent in _loopEvents)
                {
                    if (SpawnUserPreset(loopEvent.Payload) != null) continue;

                    // A missing or invalid preset stops only the affected loop, not the other performers.
                    _looperError = $"Loop {index + 1} stopped: {_error}";
                    ClearLooper(index);
                    break;
                }
            }
        }

        private void RegisterLooperMidi()
        {
            if (_midiBindings == null || _looperActionIds.Count != 0) return;

            _syncingLooperState = true;
            try
            {
                for (var index = 0; index < LooperCount; index++)
                {
                    var cycleId = LooperActionId(index, "Cycle");
                    var clearId = LooperActionId(index, "Clear");
                    _looperStates[index].Changed -= _looperStateHandlers[index];
                    _midiBindings.RegisterState(cycleId, _looperStates[index], 3, LooperColors);
                    // Loop content is session-only, so a persisted MIDI state must not restore recording/playback.
                    _looperStates[index].SetValue((int)_loopers[index].State);
                    _looperStates[index].Changed += _looperStateHandlers[index];
                    _midiBindings.RegisterMomentary(clearId, _looperClear[index]);
                    _looperActionIds.Add(cycleId);
                    _looperActionIds.Add(clearId);
                }
            }
            finally
            {
                _syncingLooperState = false;
            }
        }

        private void UnregisterLooperMidi()
        {
            if (_midiBindings == null) return;

            foreach (var id in _looperActionIds) _midiBindings.UnregisterAction(id);
            _looperActionIds.Clear();
        }

        private static string LooperActionId(int index, string action) => $"ShapeSynth.Looper.{index + 1}.{action}";

        private void RequireLooper(int index)
        {
            if (!_loopersInitialized) throw new InvalidOperationException("Loopers are not initialized.");
            if (index < 0 || index >= LooperCount) throw new ArgumentOutOfRangeException(nameof(index));
        }

        private Element BuildLoopers()
        {
            var rows = new List<Element>();
            for (var index = 0; index < LooperCount; index++)
            {
                var slot = index;
                rows.Add(UI.Row(
                    UI.Label($"Loop {slot + 1}").SetWidth(50),
                    UI.Button(UI.Label(() => LooperButtonTitle(slot)), () => AdvanceLooper(slot)).SetWidth(150),
                    _midiUi == null ? UI.Space() : _midiUi.CreateButton(LooperActionId(slot, "Cycle"), $"Loop {slot + 1}"),
                    UI.Button("Clear", () => ClearLooper(slot)),
                    _midiUi == null ? UI.Space() : _midiUi.CreateButton(LooperActionId(slot, "Clear"), $"Clear Loop {slot + 1}"),
                    UI.Label(() => LooperStatus(slot)).SetWidth(190)));
            }

            return UI.Column(
                UI.Fold("Loopers", rows),
                UI.DynamicElementIf(() => _looperError != null,
                    () => UI.Label(() => _looperError).SetColor(ErrorColor)));
        }

        private string LooperButtonTitle(int index)
        {
            if (!_loopersInitialized) return "Record";

            return _loopers[index].State switch
            {
                EventLooperState.Recording => "Recording → Play",
                EventLooperState.Playing => "Playing → Clear",
                _ => "Record"
            };
        }

        private string LooperStatus(int index)
        {
            if (!_loopersInitialized) return "Empty";

            var loop = _loopers[index];
            return loop.State == EventLooperState.Recording
                ? $"{loop.EventCount} events · recording"
                : $"{loop.EventCount} events · {loop.Duration:0.###} s";
        }
    }
}
