using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;

namespace Krab.ShapeSynth.Core.Performance
{
    public enum ModulatorKind
    {
        Adsr,
        Lfo
    }

    /// <summary>Mutable serialization DTO. Serialization is owned by the Unity layer.</summary>
    [Serializable]
    public sealed partial class PerformanceSettings
    {
        public int Version = 1;
        public List<ModulatorSettings> Modulators = new List<ModulatorSettings>();
        public Fx01Settings Fx01 = new Fx01Settings();
        public List<PresetSettings> Presets;

        public static PerformanceSettings CreateDefault()
        {
            return new PerformanceSettings
            {
                Version = 1,
                Modulators = new List<ModulatorSettings>
                {
                    new ModulatorSettings
                    {
                        Id = "fx01-envelope", Name = "FX01Envelope", Kind = ModulatorKind.Adsr,
                        Attack = 0.1, Decay = 0.5, SustainLevel = 0.5, SustainTime = 2, Release = 2
                    },
                    new ModulatorSettings
                    {
                        Id = "fx01-oscillator", Name = "FX01Oscillator", Kind = ModulatorKind.Lfo,
                        CyclesPerBeat = 0.5, PhaseOffset = 0, Waveform = LfoWaveform.Sine
                    }
                },
                Fx01 = new Fx01Settings { CircleCount = 150, EnvelopeId = "fx01-envelope", OscillatorId = "fx01-oscillator", Seed = 1234 }
            };
        }

        public ModulatorLibrary BuildLibrary()
        {
            if (Version != 1) throw new ArgumentException($"Unsupported performance settings version {Version}.", nameof(Version));
            if (Modulators == null) throw new ArgumentException("Modulators cannot be null.", nameof(Modulators));
            if (Modulators.Count < 1 || Modulators.Count > 2000)
                throw new ArgumentOutOfRangeException(nameof(Modulators), "Modulator count must be between 1 and 2000.");
            if (Fx01 == null) throw new ArgumentException("Fx01 settings cannot be null.", nameof(Fx01));
            if (Fx01.CircleCount < 1 || Fx01.CircleCount > 2000)
                throw new ArgumentOutOfRangeException(nameof(Fx01.CircleCount), "Circle count must be between 1 and 2000.");

            var entries = new List<ModulatorEntry>(Modulators.Count);
            foreach (var settings in Modulators)
            {
                if (settings == null) throw new ArgumentException("Modulators cannot contain null entries.", nameof(Modulators));
                switch (settings.Kind)
                {
                    case ModulatorKind.Adsr:
                        entries.Add(new ModulatorEntry(settings.Id, settings.Name,
                            new AdsrDefinition(settings.Attack, settings.Decay, settings.SustainLevel, settings.SustainTime, settings.Release)));
                        break;
                    case ModulatorKind.Lfo:
                        entries.Add(new ModulatorEntry(settings.Id, settings.Name,
                            new LfoDefinition(settings.CyclesPerBeat, settings.PhaseOffset, settings.Waveform)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Modulators), $"Unsupported modulator kind '{settings.Kind}'.");
                }
            }

            var library = new ModulatorLibrary(entries);
            if (library.Get(Fx01.EnvelopeId).Adsr == null)
                throw new ArgumentException("Fx01.EnvelopeId must refer to an ADSR modulator.", nameof(Fx01));
            if (library.Get(Fx01.OscillatorId).Lfo == null)
                throw new ArgumentException("Fx01.OscillatorId must refer to an LFO modulator.", nameof(Fx01));
            library.ResolveSlots(Fx01.EnvelopeId, Fx01.OscillatorId);
            ValidatePresets(library);
            return library;
        }
    }

    [Serializable]
    public sealed class ModulatorSettings
    {
        public string Id;
        public string Name;
        public ModulatorKind Kind;
        public double Attack;
        public double Decay;
        public double SustainLevel;
        public double SustainTime;
        public double Release;
        public double CyclesPerBeat;
        public double PhaseOffset;
        public LfoWaveform Waveform;
    }

    [Serializable]
    public sealed class Fx01Settings
    {
        public int CircleCount = 150;
        public string EnvelopeId = "fx01-envelope";
        public string OscillatorId = "fx01-oscillator";
        public int Seed = 1234;
    }
}
