using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>One trigger's LFO. Sampling has no side effects and uses the captured BPM.</summary>
    public sealed class LfoInstance : IModulator
    {
        public LfoDefinition Definition { get; }
        public VoiceTime Time { get; }

        public LfoInstance(LfoDefinition definition, VoiceTime time)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            time.ElapsedBeats(time.StartSeconds); // Reject default(VoiceTime), as ADSR does.
            Time = time;
        }

        public double Evaluate(double nowSeconds)
        {
            var beats = Time.ElapsedBeats(nowSeconds);
            VoiceTime.RequireFinite(beats, nameof(nowSeconds));

            var frequency = Definition.CyclesPerBeat;
            double cycles = 0;
            if (frequency > 0)
            {
                var periodBeats = 1 / frequency;
                // Reduce before multiplying: avoids huge trig arguments and beats*frequency overflow.
                // For tiny frequencies whose period overflows, the direct product is below one.
                cycles = double.IsPositiveInfinity(periodBeats)
                    ? beats * frequency
                    : (beats % periodBeats) * frequency;
            }

            var phase = LfoDefinition.WrapPhase(cycles + Definition.PhaseOffset);
            switch (Definition.Waveform)
            {
                case LfoWaveform.Sine:
                    return Math.Sin(2 * Math.PI * phase);
                case LfoWaveform.Triangle:
                    return 1 - 4 * Math.Abs(LfoDefinition.WrapPhase(phase + 0.25) - 0.5);
                case LfoWaveform.Sawtooth:
                    return 2 * phase - 1;
                case LfoWaveform.Square:
                    return phase < 0.5 ? 1 : -1;
                default:
                    throw new InvalidOperationException("Unknown LFO waveform.");
            }
        }
    }
}
