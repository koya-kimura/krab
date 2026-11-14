using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;

namespace Krab.ShapeSynth.Core.Performance
{
    public sealed partial class PerformanceSettings
    {
        private static readonly string[] PerformancePresetIds = { "pad01", "snare01", "lead01", "lead03" };

        public static PerformanceSettings CreateForPerformance()
        {
            var settings = CreateDefault();
            settings.EnsureAdditionalPresets();
            return settings;
        }

        public void EnsureAdditionalPresets()
        {
            if (Presets != null && Presets.Count > 0) return;

            Presets = new List<PresetSettings>
            {
                new PresetSettings { Id = "pad01", EnvelopeId = "pad01-envelope", OscillatorId = "pad01-lfo", Seed = 1234 },
                new PresetSettings { Id = "snare01", EnvelopeId = "snare01-envelope", Seed = 1234 },
                new PresetSettings { Id = "lead01", EnvelopeId = "lead01-envelope", OscillatorId = "lead01-lfo", Seed = 1234 },
                new PresetSettings { Id = "lead03", EnvelopeId = "lead03-envelope", Seed = 1234 }
            };

            AddAdsrIfMissing("pad01-envelope", "Pad01Envelope", 0.8, 0.2, 0.8, 2, 2);
            AddAdsrIfMissing("snare01-envelope", "Snare01Envelope", 0.05, 0.1, 0.8, 0.1, 0.5);
            AddAdsrIfMissing("lead01-envelope", "Lead01Envelope", 0.2, 0.2, 0.7, 0.8, 1);
            AddAdsrIfMissing("lead03-envelope", "Lead03Envelope", 0.2, 0.2, 1, 1, 1);
            AddLfoIfMissing("pad01-lfo", "Pad01Lfo", 0.5);
            AddLfoIfMissing("lead01-lfo", "Lead01Lfo", 1.0);
        }

        private void AddAdsrIfMissing(string id, string name, double attack, double decay, double sustain, double sustainTime, double release)
        {
            if (Modulators.Exists(entry => entry != null && entry.Id == id)) return;
            Modulators.Add(new ModulatorSettings
            {
                Id = id, Name = name, Kind = ModulatorKind.Adsr,
                Attack = attack, Decay = decay, SustainLevel = sustain, SustainTime = sustainTime, Release = release
            });
        }

        private void AddLfoIfMissing(string id, string name, double cyclesPerBeat)
        {
            if (Modulators.Exists(entry => entry != null && entry.Id == id)) return;
            Modulators.Add(new ModulatorSettings
            {
                Id = id, Name = name, Kind = ModulatorKind.Lfo,
                CyclesPerBeat = cyclesPerBeat, PhaseOffset = 0, Waveform = LfoWaveform.Sine
            });
        }

        private void ValidatePresets(ModulatorLibrary library)
        {
            if (Presets == null || Presets.Count == 0) return;
            if (Presets.Count != PerformancePresetIds.Length)
                throw new ArgumentException("Presets must contain exactly pad01, snare01, lead01, and lead03.", nameof(Presets));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var preset in Presets)
            {
                if (preset == null) throw new ArgumentException("Presets cannot contain null entries.", nameof(Presets));
                if (!Array.Exists(PerformancePresetIds, id => id == preset.Id) || !ids.Add(preset.Id))
                    throw new ArgumentException($"Unknown or duplicate preset ID '{preset.Id}'.", nameof(Presets));
                if (library.Get(preset.EnvelopeId).Adsr == null)
                    throw new ArgumentException($"Preset '{preset.Id}' must reference an ADSR envelope.", nameof(Presets));
                if (!string.IsNullOrEmpty(preset.OscillatorId) && library.Get(preset.OscillatorId).Lfo == null)
                    throw new ArgumentException($"Preset '{preset.Id}' oscillator must reference an LFO.", nameof(Presets));
            }
        }
    }

    [Serializable]
    public sealed class PresetSettings
    {
        public string Id;
        public string EnvelopeId;
        public string OscillatorId;
        public int Seed = 1234;
    }
}
