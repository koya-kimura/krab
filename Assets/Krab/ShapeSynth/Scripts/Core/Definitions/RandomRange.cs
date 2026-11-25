using System;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// A seeded uniform offset in [Min, Max]. It is added to a fixed value (Base, Depth or Delay) once per
    /// occurrence when the evaluator is built, so it differs per Voice seed and per placement, never per frame.
    /// With Steps of 2 or more it is one of Steps evenly spaced values from Min to Max, both included.
    /// </summary>
    public sealed class RandomRange
    {
        public double Min { get; }
        public double Max { get; }
        public uint Salt { get; }

        /// <summary>0 or 1: any value in the range. 2 or more: that many evenly spaced values.</summary>
        public int Steps { get; }

        public const int MaxSteps = 1000;

        public RandomRange(double min, double max, uint salt = 0, int steps = 0)
        {
            if (steps < 0 || steps > MaxSteps)
                throw new ArgumentOutOfRangeException(nameof(steps), $"Steps must be 0–{MaxSteps}.");
            VoiceTime.RequireFinite(min, nameof(min));
            VoiceTime.RequireFinite(max, nameof(max));
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than or equal to minimum.");

            Min = min;
            Max = max;
            Salt = salt;
            Steps = steps;
        }

        /// <summary>Symmetric ±spread around 0.</summary>
        public static RandomRange Spread(double spread, uint salt = 0, int steps = 0)
        {
            VoiceTime.RequireFinite(spread, nameof(spread));
            var half = Math.Abs(spread);
            return new RandomRange(-half, half, salt, steps);
        }

        public double Resolve(RandomContext context, ulong channel = 0)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (Steps < 2) return SeededRandom.Range(Min, Max, context, channel, Salt);

            // Range is [0, Steps) but rounding can give Steps itself, so the index is clamped.
            var index = Math.Min(Steps - 1, (int)Math.Floor(SeededRandom.Range(0, Steps, context, channel, Salt)));
            return Min + (Max - Min) * index / (Steps - 1);
        }
    }
}
