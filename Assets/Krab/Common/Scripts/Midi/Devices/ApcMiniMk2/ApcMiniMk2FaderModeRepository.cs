using System.Collections.Generic;
using PrefsGUI;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    public sealed class ApcMiniMk2FaderModeRepository
    {
        private readonly Dictionary<int, Entry> _entries = new();

        public FaderModeAssignment LoadAssignment(int index)
        {
            var prefs = GetEntry(index).Assignment;
            var value = prefs.Get();
            if (value == (int)FaderModeAssignment.Mute || value == (int)FaderModeAssignment.Random)
                return (FaderModeAssignment)value;
            prefs.Set((int)FaderModeAssignment.Mute);
            return FaderModeAssignment.Mute;
        }

        public bool LoadActive(int index) => GetEntry(index).Active.Get();
        public void SaveAssignment(int index, FaderModeAssignment value) => GetEntry(index).Assignment.Set((int)value);
        public void SaveActive(int index, bool value) => GetEntry(index).Active.Set(value);

        private Entry GetEntry(int index)
        {
            ApcMiniMk2.ValidateChannelFaderIndex(index);
            if (!_entries.TryGetValue(index, out var entry))
                _entries.Add(index, entry = new Entry(index));
            return entry;
        }

        private sealed class Entry
        {
            public Entry(int index)
            {
                var prefix = $"Midi.ApcMiniMk2.Fader.{index}";
                Assignment = new PrefsInt($"{prefix}.Assignment", (int)FaderModeAssignment.Mute);
                Active = new PrefsBool($"{prefix}.Active", false);
            }

            public PrefsInt Assignment { get; }
            public PrefsBool Active { get; }
        }
    }
}
