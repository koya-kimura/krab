using System;
using System.Collections.Generic;

namespace Krab.TypeLayout.Core.Text
{
    public static class TextSequence
    {
        /// <summary>
        /// Fills slots by repeating the clusters in order, starting from <paramref name="start"/> (wraps either way).
        /// </summary>
        public static void Cycle(IReadOnlyList<string> clusters, int count, int start, List<string> results)
        {
            Require.NotNull(clusters, nameof(clusters));
            Require.NotNull(results, nameof(results));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), count, null);
            if (count > 0 && clusters.Count == 0) throw new ArgumentException("No clusters to repeat.", nameof(clusters));

            results.Clear();
            for (var i = 0; i < count; i++)
                results.Add(clusters[Wrap(start + i, clusters.Count)]);
        }

        public static int Wrap(int index, int count)
        {
            var wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
