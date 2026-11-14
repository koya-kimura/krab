using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Presets
{
    public sealed class PresetDescriptor
    {
        public string Id { get; }
        public string Name { get; }
        public char Key { get; }
        internal PresetDescriptor(string id, string name, char key) { Id = id; Name = name; Key = key; }
    }

    /// <summary>The five sample presets, independent of Unity input and Modulator IDs.</summary>
    public static class PresetCatalog
    {
        public static IReadOnlyList<PresetDescriptor> Entries { get; } = Array.AsReadOnly(new[]
        {
            new PresetDescriptor("fx01", "FX01", 'q'), new PresetDescriptor("pad01", "Pad01", 'w'),
            new PresetDescriptor("snare01", "Snare01", 'e'), new PresetDescriptor("lead01", "Lead01", 'a'),
            new PresetDescriptor("lead03", "Lead03", 's')
        });

        public static string IdForKey(char key)
        {
            foreach (var preset in Entries) if (preset.Key == char.ToLowerInvariant(key)) return preset.Id;
            return null;
        }

        public static PresetSettings SettingsFor(PerformanceSettings settings, string id)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.Presets != null)
                foreach (var preset in settings.Presets) if (preset.Id == id) return preset;
            throw new ArgumentException("Unknown preset ID: " + id, nameof(id));
        }

        public static int SeedFor(PerformanceSettings settings, string id) =>
            id == "fx01" ? settings.Fx01.Seed : SettingsFor(settings, id).Seed;

        public static TriggerTarget Build(PerformanceSettings settings, string id, int seed)
        {
            var library = settings.BuildLibrary();
            if (id == "fx01") return Fx01Preset.Build(library, settings.Fx01);
            var preset = SettingsFor(settings, id);
            switch (id)
            {
                case "pad01": return AdditionalPresets.Pad01(library, preset, seed);
                case "snare01": return AdditionalPresets.Snare01(library, preset, seed);
                case "lead01": return AdditionalPresets.Lead01(library, preset, seed);
                case "lead03": return AdditionalPresets.Lead03(library, preset, seed);
                default: throw new ArgumentException("Unknown preset ID: " + id, nameof(id));
            }
        }
    }
}
