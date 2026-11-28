using System;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Core.Definitions
{
    public enum LfoWaveform
    {
        Sine,
        Triangle,
        Sawtooth,
        Square
    }

    /// <summary>Immutable signal settings. Frequency is cycles/beat; offset is in cycles.</summary>
    public sealed class LfoDefinition : IModulatorDefinition
    {
        public double CyclesPerBeat { get; }
        public double PhaseOffset { get; }
        public LfoWaveform Waveform { get; }

        public LfoDefinition(double cyclesPerBeat, double phaseOffset = 0, LfoWaveform waveform = LfoWaveform.Sine)
        {
            VoiceTime.RequireFinite(cyclesPerBeat, nameof(cyclesPerBeat));
            if (cyclesPerBeat < 0) throw new ArgumentOutOfRangeException(nameof(cyclesPerBeat));

            VoiceTime.RequireFinite(phaseOffset, nameof(phaseOffset));
            if (waveform < LfoWaveform.Sine || waveform > LfoWaveform.Square)
                throw new ArgumentOutOfRangeException(nameof(waveform));

            CyclesPerBeat = cyclesPerBeat;
            PhaseOffset = WrapPhase(phaseOffset);
            Waveform = waveform;
        }

        public IModulator Create(VoiceTime time) => new LfoInstance(this, time);

        internal static double WrapPhase(double cycles)
        {
            var phase = cycles % 1.0;
            if (phase < 0) phase += 1;
            // Adding one to a tiny negative remainder can round to exactly one.
            return phase >= 1 ? 0 : phase;
        }
    }
}
