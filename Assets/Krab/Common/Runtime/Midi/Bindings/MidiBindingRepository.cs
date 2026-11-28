using System.Collections.Generic;
using Krab.Common.Midi.Core;
using PrefsGUI;
using UnityEngine;

namespace Krab.Common.Midi.Bindings
{
    public sealed class MidiBindingRepository
    {
        private readonly Dictionary<(string, MidiDeviceKind), Entry> _entries = new();
        private readonly Dictionary<string, PrefsColor> _ledColors = new();

        public MidiControl? Load(string paramId, MidiDeviceKind device)
        {
            var entry = GetEntry(paramId, device);

            var type = entry.Type.Get();
            var number = entry.Number.Get();
            if (type < 0 || number < 0) return null;

            return new MidiControl((MidiMessageType)type, entry.Channel.Get(), number, device);
        }

        public PrefsColor GetLedColor(string paramId, Color defaultColor)
        {
            if (_ledColors.TryGetValue(paramId, out var color)) return color;

            color = new PrefsColor($"Midi.Binding.{paramId}.LedColor", defaultColor);
            _ledColors.Add(paramId, color);
            return color;
        }

        public void Save(string paramId, MidiDeviceKind device, MidiControl? control)
        {
            var entry = GetEntry(paramId, device);

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

        private Entry GetEntry(string paramId, MidiDeviceKind device)
        {
            if (!_entries.TryGetValue((paramId, device), out var entry))
            {
                entry = new Entry(GetPrefix(paramId, device));
                _entries.Add((paramId, device), entry);
            }

            return entry;
        }

        // APC mini mk2 keeps the keys saved before multi-device support.
        private static string GetPrefix(string paramId, MidiDeviceKind device)
        {
            return device == MidiDeviceKind.ApcMiniMk2
                ? $"Midi.Binding.{paramId}"
                : $"Midi.Binding.{paramId}.{device}";
        }

        private sealed class Entry
        {
            public readonly PrefsInt Channel;
            public readonly PrefsInt Number;
            public readonly PrefsInt Type;

            public Entry(string prefix)
            {
                Type = new PrefsInt($"{prefix}.Type", -1);
                Channel = new PrefsInt($"{prefix}.Channel");
                Number = new PrefsInt($"{prefix}.Number", -1);
            }
        }
    }
}
