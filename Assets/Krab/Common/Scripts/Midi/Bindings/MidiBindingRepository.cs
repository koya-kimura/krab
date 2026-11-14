using System.Collections.Generic;
using Krab.Common.Midi.Core;
using PrefsGUI;
using UnityEngine;

namespace Krab.Common.Midi.Bindings
{
    public sealed class MidiBindingRepository
    {
        private readonly Dictionary<string, Entry> _entries = new();

        public MidiControl? Load(string paramId)
        {
            var entry = GetEntry(paramId);

            var type = entry.Type.Get();
            var number = entry.Number.Get();
            if (type < 0 || number < 0) return null;

            return new MidiControl((MidiMessageType)type, entry.Channel.Get(), number);
        }

        public PrefsColor GetLedColor(string paramId, Color defaultColor)
        {
            var entry = GetEntry(paramId);
            return entry.LedColor ??= new PrefsColor($"Midi.Binding.{paramId}.LedColor", defaultColor);
        }

        public void Save(string paramId, MidiControl? control)
        {
            var entry = GetEntry(paramId);

            if (!control.HasValue)
            {
                entry.Type.Set(-1);
                entry.Number.Set(-1);
                return;
            }

            entry.Type.Set((int)control.Value.Type);
            entry.Channel.Set(control.Value.Channel);
            entry.Number.Set(control.Value.Number);
        }

        private Entry GetEntry(string paramId)
        {
            if (!_entries.TryGetValue(paramId, out var entry))
            {
                entry = new Entry(paramId);
                _entries.Add(paramId, entry);
            }

            return entry;
        }

        private sealed class Entry
        {
            public readonly PrefsInt Channel;
            public readonly PrefsInt Number;
            public readonly PrefsInt Type;
            public PrefsColor LedColor;

            public Entry(string paramId)
            {
                var prefix = $"Midi.Binding.{paramId}";

                Type = new PrefsInt($"{prefix}.Type", -1);
                Channel = new PrefsInt($"{prefix}.Channel");
                Number = new PrefsInt($"{prefix}.Number", -1);
            }
        }
    }
}
