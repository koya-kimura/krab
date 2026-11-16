using System;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// A seeded uniform offset in [Min, Max]. It is added to a fixed value (Base, Depth or Delay) once per
    /// occurrence when the evaluator is built, so it differs per Voice seed and per placement, never per frame.
    /// </summary>
    public sealed class RandomRange
    {
        public double Min { get; }
        public double Max { get; }
        public uint Salt { get; }

        public RandomRange(double min, double max, uint salt = 0)
        {
            VoiceTime.RequireFinite(min, nameof(min));
            VoiceTime.RequireFinite(max, nameof(max));
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than or equal to minimum.");

            Min = min;
            Max = max;
            Salt = salt;
        }

        /// <summary>Symmetric ±spread around 0.</summary>
        public static RandomRange Spread(double spread, uint salt = 0)
        {
            VoiceTime.RequireFinite(spread, nameof(spread));
            var half = Math.Abs(spread);
            return new RandomRange(-half, half, salt);
        }

        public double Resolve(RandomContext context, ulong channel = 0)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return SeededRandom.Range(Min, Max, context, channel, Salt);
        }
    }
}
