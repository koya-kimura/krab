using System;
using System.Collections.Generic;

namespace Krab.TypeLayout.Core.Text
{
    /// <summary>Chooses which cluster goes to each slot.</summary>
    public static class TextPicker
    {
        /// <summary>
        /// Picks a cluster per slot from a seeded hash of (seed, slot, step). The same inputs always give the same
        /// result on every platform; changing <paramref name="step"/> re-rolls every slot.
        /// </summary>
        public static void Random(IReadOnlyList<string> clusters, int count, int seed, long step, List<string> results)
        {
            Require.NotNull(clusters, nameof(clusters));
            Require.NotNull(results, nameof(results));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, null);
            if (count > 0 && clusters.Count == 0) throw new ArgumentException("No clusters to pick.", nameof(clusters));

            results.Clear();
            for (var i = 0; i < count; i++)
                results.Add(clusters[(int)(Hash(seed, i, step) % (ulong)clusters.Count)]);
        }

        // SplitMix64 finalizer over the packed inputs: stable across runtimes, unlike System.Random.
        public static ulong Hash(int seed, int slot, long step)
        {
            var value = (ulong)(uint)seed;
            value = Mix(value ^ ((ulong)(uint)slot << 32));
            value = Mix(value ^ (ulong)step);
            return value;
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
