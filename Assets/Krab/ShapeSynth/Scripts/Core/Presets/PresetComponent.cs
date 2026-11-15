using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>One ADSR or LFO inside a Value. Only the fields of its Kind are used.</summary>
    [Serializable]
    public sealed class PresetComponent
    {
        public ModulatorKind Kind;
        public double Weight = 1;
        public double Attack = 0.1;
        public double Decay = 0.2;
        public double SustainLevel = 0.8;
        public double SustainTime = 1;
        public double Release = 1;
        public double CyclesPerBeat = 0.5;
        public double PhaseOffset;
        public LfoWaveform Waveform;

        public static PresetComponent Create(ModulatorKind kind)
        {
            return new PresetComponent { Kind = kind };
        }
    }
}
