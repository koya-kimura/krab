using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>
    /// One trigger's rise. 0 before the trigger, Curve(elapsed / duration) during [0, duration), and 0 from the
    /// duration on: it never holds 1. Sampling is side-effect free.
    /// </summary>
    public sealed class RampInstance : IDelayableModulator, ILifetime
    {
        public RampDefinition Definition { get; }
        public VoiceTime Time { get; }

        public RampInstance(RampDefinition definition, VoiceTime time)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            time.ElapsedBeats(time.StartSeconds); // Reject an uninitialized snapshot, as ADSR does.
            Time = time;
        }

        public double Evaluate(double nowSeconds)
        {
            var beats = Time.ElapsedBeats(nowSeconds);
            if (nowSeconds < Time.StartSeconds || beats >= Definition.DurationBeats) return 0;

            return EasingFunctions.Evaluate(Definition.Curve, beats / Definition.DurationBeats);
        }

        /// <summary>Reads the rise delayBeats earlier, at the Voice's start BPM.</summary>
        public double EvaluateDelayed(double nowSeconds, double delayBeats) =>
            Evaluate(nowSeconds - delayBeats * 60.0 / Time.StartBpm);

        public bool IsFinished(double nowSeconds)
        {
            var beats = Time.ElapsedBeats(nowSeconds);
            return nowSeconds >= Time.StartSeconds && beats >= Definition.DurationBeats;
        }
    }
}
