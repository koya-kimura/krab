using System;
using System.Collections.Generic;
using Krab.Common.Looping.Core;
using Krab.Common.Looping.Midi;
using Krab.Common.Looping.Views;
using Krab.ShapeSynth.Core.Voices;
using PrefsGUI;
using RosettaUI;
using UnityEngine;

namespace Krab.ShapeSynth.Performance
{
    public sealed partial class ShapeSynthPerformance
    {
        public const int LooperCount = 8;

        private const string LooperActionPrefix = "ShapeSynth.Looper";
        private const double BeatsPerBar = 4;

        private readonly List<LoopPlaybackEvent<LoopNote>> _loopEvents = new();
        private readonly PrefsBool[] _loopBpmSync = CreateLoopBpmSyncPrefs();

        // Voices a playing loop is holding, per slot and by the HoldId recorded with the note.
        private readonly Dictionary<int, VoiceInstance>[] _loopHolds = CreateLoopHolds();

        private LooperBank<LoopNote> _loopers;
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

            EndAllHolds();
            Loopers.ClearAll();
            DetachLooperMidi();
        }

        public LooperBank<LoopNote> Loopers => _loopers ??= new LooperBank<LoopNote>(
            LooperCount, LooperClock, slot => IsLoopBpmSynced(slot) ? BeatsPerBar : 0);

        public string LooperError => _looperError;

        public void AdvanceLooper(int index)
        {
            if (!isActiveAndEnabled) return;

            Loopers.Cycle(index);
        }

        public void ClearLooper(int index)
        {
            Loopers.Clear(index);
        }

        public bool IsLoopBpmSynced(int index) => _loopBpmSync[index];

        /// <summary>
        /// Free loops keep their length in real seconds like the p5 looper; BPM-synced loops count beats, snap
        /// to whole bars and follow tempo changes. Switching clears the slot, since its positions change unit.
        /// </summary>
        public void SetLoopBpmSync(int index, bool synced)
        {
            if (_loopBpmSync[index] == synced) return;

            Loopers.Clear(index);
            _loopBpmSync[index].Set(synced);
        }

        private double LooperClock(int slot)
        {
            return IsLoopBpmSynced(slot) && _bpm != null ? _bpm.Beat : Time.unscaledTimeAsDouble;
        }

        private void InitializeLoopers()
        {
            _loopersStarted = true;
            Loopers.StateChanged += OnLooperStateChanged;
            AttachLooperMidi();
        }

        private void OnLooperStateChanged(int index)
        {
            var state = Loopers.GetState(index);
            if (state == EventLooperState.Recording) _looperError = null;
            if (state != EventLooperState.Playing) ReleaseLoopHolds(index);
        }

        private void RecordLoopNote(LoopNote note)
        {
            if (!_loopersStarted || !isActiveAndEnabled) return;

            Loopers.Record(note);
        }

        private void UpdateLoopers()
        {
            if (!_loopersStarted) return;

            for (var index = 0; index < LooperCount; index++)
            {
                var dropped = Loopers[index].DroppedEventCount;
                _loopEvents.Clear();
                Loopers.Advance(index, _loopEvents);
                if (Loopers[index].DroppedEventCount != dropped)
                    _looperError = $"Loop {index + 1}: skipped overdue events after a long frame.";

                foreach (var loopEvent in _loopEvents)
                {
                    // Playback spawns without recording, so a replay never feeds another recording slot.
                    if (PlayLoopNote(index, loopEvent.Payload)) continue;

                    // A missing or invalid preset stops only the affected loop, not the other performers.
                    _looperError = $"Loop {index + 1} stopped: {_error}";
                    Loopers.Clear(index);
                    break;
                }
            }
        }

        private bool PlayLoopNote(int slot, LoopNote note)
        {
            var holds = _loopHolds[slot];
            switch (note.Kind)
            {
                case LoopNoteKind.HoldStart:
                    // A hold longer than the loop is cut where the loop comes round to its start again.
                    if (holds.Remove(note.HoldId, out var previous)) _controller.Release(previous);

                    var voice = SpawnUserPreset(note.PresetId, false);
                    if (voice == null) return false;

                    holds.Add(note.HoldId, voice);
                    return true;
                case LoopNoteKind.HoldEnd:
                    // An end without its start (pressed before recording, or wrapped by bar snapping) is a no-op.
                    if (holds.Remove(note.HoldId, out var held)) _controller.Release(held);

                    return true;
                default:
                    return SpawnUserPreset(note.PresetId) != null;
            }
        }

        private void ReleaseLoopHolds(int slot)
        {
            var holds = _loopHolds[slot];
            if (holds.Count == 0) return;

            if (_controller)
            {
                foreach (var voice in holds.Values) _controller.Release(voice);
            }

            holds.Clear();
        }

        private void AttachLooperMidi()
        {
            if (_midiBindings == null || _looperMidi != null) return;

            _looperMidi = new LooperBankMidi(_midiBindings, Loopers, LooperActionPrefix);
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
                LooperBankUi.Create(Loopers, slot => IsLoopBpmSynced(slot) ? "beats" : "s", cycleMidi, clearMidi,
                    LoopSyncButton),
                UI.DynamicElementIf(() => _looperError != null,
                    () => UI.Label(() => _looperError).SetColor(ErrorColor)));
        }

        private Element LoopSyncButton(int slot)
        {
            return UI.Button(UI.Label(() => IsLoopBpmSynced(slot) ? "Sync: BPM" : "Sync: Free"),
                () => SetLoopBpmSync(slot, !IsLoopBpmSynced(slot))).SetWidth(100);
        }

        private static PrefsBool[] CreateLoopBpmSyncPrefs()
        {
            var prefs = new PrefsBool[LooperCount];
            for (var slot = 0; slot < LooperCount; slot++)
                prefs[slot] = new PrefsBool($"{LooperActionPrefix}.{slot + 1}.BpmSync", false);
            return prefs;
        }

        private static Dictionary<int, VoiceInstance>[] CreateLoopHolds()
        {
            var holds = new Dictionary<int, VoiceInstance>[LooperCount];
            for (var slot = 0; slot < LooperCount; slot++) holds[slot] = new Dictionary<int, VoiceInstance>();
            return holds;
        }
    }
}
