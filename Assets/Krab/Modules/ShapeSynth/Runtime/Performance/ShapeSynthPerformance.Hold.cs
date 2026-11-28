using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Performance
{
    /// <summary>
    /// Held presets: the Voice is compiled without AutoRelease, so its ADSRs stay at Sustain until EndHold.
    /// Start and end are recorded with a shared HoldId, so loops replay the held length.
    /// </summary>
    public sealed partial class ShapeSynthPerformance
    {
        private readonly Dictionary<string, (VoiceInstance Voice, int HoldId)> _liveHolds =
            new(StringComparer.Ordinal);

        private int _nextHoldId;

        public bool IsHeld(string presetId) => _liveHolds.ContainsKey(presetId);

        public VoiceInstance StartHold(string presetId)
        {
            if (!isActiveAndEnabled) return null;

            // A second press before the release (e.g. a lost Note Off) ends the previous hold first.
            EndHold(presetId);
            var voice = SpawnUserPreset(presetId, false);
            if (voice == null) return null;

            var holdId = _nextHoldId++;
            _liveHolds.Add(presetId, (voice, holdId));
            RecordLoopNote(LoopNote.HoldStart(presetId, holdId));
            return voice;
        }

        public void EndHold(string presetId)
        {
            if (!_liveHolds.Remove(presetId, out var hold)) return;

            if (_controller) _controller.Release(hold.Voice);
            RecordLoopNote(LoopNote.HoldEnd(presetId, hold.HoldId));
        }

        private void EndAllHolds()
        {
            foreach (var presetId in _liveHolds.Keys.ToList()) EndHold(presetId);
        }
    }
}
