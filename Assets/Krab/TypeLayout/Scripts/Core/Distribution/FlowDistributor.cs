using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Paths;

namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>
    /// Runs text along a path by the advance of each glyph. Glyphs follow the path one by one, so pair kerning
    /// is not applied.
    /// </summary>
    public static class FlowDistributor
    {
        public const int MaxGlyphs = 10000;

        /// <summary>
        /// Flows clusters whose advances (output pixels) are given in reading order into <paramref name="results"/>.
        /// </summary>
        /// <returns>Spacing actually used between glyphs (differs from the tracking when justified).</returns>
        public static double Flow(ILayoutPath path, IReadOnlyList<double> advances, FlowOptions options,
            List<FlowItem> results)
        {
            Require.NotNull(path, nameof(path));
            Require.NotNull(advances, nameof(advances));
            Require.NotNull(results, nameof(results));
            results.Clear();
            if (advances.Count == 0) return options.Tracking;

            var advanceSum = 0.0;
            for (var i = 0; i < advances.Count; i++)
                advanceSum += Require.NonNegative(advances[i], nameof(advances));

            var count = GlyphCount(path, advances, advanceSum, options);
            if (count == 0) return options.Tracking;

            var total = RepeatedAdvance(advances, advanceSum, count);
            var gaps = path.IsClosed ? count : count - 1;
            var spacing = options.Tracking;
            if (options.Alignment == FlowAlignment.Justify && gaps > 0) spacing = (path.Length - total) / gaps;

            var start = StartDistance(path, total + spacing * (count - 1), options.Alignment, gaps) + options.Offset;
            var distance = start;
            for (var i = 0; i < count; i++)
            {
                var cluster = i % advances.Count;
                var center = distance + advances[cluster] / 2;
                if (!IsClipped(path, center, advances[cluster], options))
                    results.Add(new FlowItem(cluster, center, path.Sample(center)));

                distance += advances[cluster] + spacing;
            }

            return spacing;
        }

        private static int GlyphCount(ILayoutPath path, IReadOnlyList<double> advances, double advanceSum,
            FlowOptions options)
        {
            if (options.Fill == FlowFill.None) return advances.Count;

            if (advanceSum + options.Tracking * advances.Count <= 0)
                throw new ArgumentException("Repeating needs glyphs that take up space.", nameof(advances));

            // A closed loop pays one extra gap at the seam; an open path does not.
            var gapBudget = path.IsClosed ? 0 : options.Tracking;
            var count = 0;
            var used = 0.0;
            while (count < MaxGlyphs)
            {
                var next = used + advances[count % advances.Count] + options.Tracking;
                if (next > path.Length + gapBudget + Epsilon(path)) break;

                used = next;
                count++;
            }

            if (count >= MaxGlyphs)
                throw new ArgumentException($"Repeating exceeds {MaxGlyphs} glyphs.", nameof(advances));

            if (options.Fill == FlowFill.WholeText) count -= count % advances.Count;

            // Always show the text at least once, even if it overflows.
            return Math.Max(count, advances.Count);
        }

        private static double RepeatedAdvance(IReadOnlyList<double> advances, double advanceSum, int count)
        {
            var total = advanceSum * (count / advances.Count);
            for (var i = 0; i < count % advances.Count; i++)
                total += advances[i];

            return total;
        }

        private static double StartDistance(ILayoutPath path, double span, FlowAlignment alignment, int gaps)
        {
            if (path.IsClosed)
            {
                return alignment switch
                {
                    FlowAlignment.Center => -span / 2,
                    FlowAlignment.End => -span,
                    _ => 0
                };
            }

            return alignment switch
            {
                FlowAlignment.Center => (path.Length - span) / 2,
                FlowAlignment.End => path.Length - span,
                FlowAlignment.Justify when gaps == 0 => (path.Length - span) / 2,
                _ => 0
            };
        }

        private static bool IsClipped(ILayoutPath path, double center, double advance, FlowOptions options)
        {
            if (path.IsClosed || options.KeepOverflow) return false;

            var epsilon = Epsilon(path);
            return center - advance / 2 < -epsilon || center + advance / 2 > path.Length + epsilon;
        }

        private static double Epsilon(ILayoutPath path) => path.Length * 1e-9;
    }
}
