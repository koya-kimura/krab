using System;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>
    /// Deterministic hash-based random: the same seed, path, channel, salt and range always give the same value.
    /// Not cryptographic. Never uses UnityEngine.Random or a sequential generator.
    /// </summary>
    public static class SeededRandom
    {
        /// <summary>A value in [min, max); rounding can produce max. min = max returns min.</summary>
        public static double Range(double min, double max, RandomContext context, ulong channel = 0, uint salt = 0)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (min > max) throw new ArgumentOutOfRangeException(nameof(max));
            if (min == max) return min;

            var hash = Mix64(unchecked((ulong)(long)context.VoiceSeed) ^ 0x9E3779B97F4A7C15UL);
            hash = Mix64(hash ^ unchecked((ulong)context.Path.Count));
            for (var i = 0; i < context.Path.Count; i++)
            {
                hash = Mix64(hash ^ unchecked((ulong)context.Path[i]));
            }

            hash = Mix64(hash ^ channel);
            hash = Mix64(hash ^ salt);
            hash = Mix64(hash ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(min)));
            hash = Mix64(hash ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(max)));

            var unit = (hash >> 11) * (1.0 / 9007199254740992.0);
            var value = min * (1.0 - unit) + max * unit;
            if (double.IsNaN(value)) throw new InvalidOperationException("Random interpolation produced NaN.");
            return Math.Max(min, Math.Min(max, value));
        }

        private static ulong Mix64(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
