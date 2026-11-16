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
            Require.NotNull(advances, nameof(advances));
            var glyphs = new FlowGlyph[advances.Count];
            for (var i = 0; i < glyphs.Length; i++)
                glyphs[i] = new FlowGlyph(advances[i]);

            return Flow(path, glyphs, options, results);
        }

        /// <summary>Flows glyphs in reading order into <paramref name="results"/>.</summary>
        /// <returns>Spacing actually used between glyphs (differs from the tracking when justified).</returns>
        public static double Flow(ILayoutPath path, IReadOnlyList<FlowGlyph> glyphs, FlowOptions options,
            List<FlowItem> results)
        {
            Require.NotNull(path, nameof(path));
            Require.NotNull(glyphs, nameof(glyphs));
            Require.NotNull(results, nameof(results));
            results.Clear();
            if (glyphs.Count == 0) return options.Tracking;

            var advanceSum = 0.0;
            for (var i = 0; i < glyphs.Count; i++)
                advanceSum += glyphs[i].Advance;

            var count = GlyphCount(path, glyphs, advanceSum, options);
            var total = RepeatedAdvance(glyphs, advanceSum, count);
            var gaps = path.IsClosed ? count : count - 1;

            // Ink flush trims the outer bearings so the drawn shapes, not the advance boxes, meet the path ends.
            var flush = options.InkFlush && !path.IsClosed;
            var leadingTrim = flush ? glyphs[0].LeadingBearing : 0;
            var trailingTrim = flush ? glyphs[(count - 1) % glyphs.Count].TrailingBearing : 0;

            var spacing = options.Tracking;
            if (options.Alignment == FlowAlignment.Justify && gaps > 0)
                spacing = (path.Length - total + leadingTrim + trailingTrim) / gaps;

            var span = total + spacing * (count - 1) - leadingTrim - trailingTrim;
            var distance = StartDistance(path, span, options.Alignment, gaps) - leadingTrim + options.Offset;
            for (var i = 0; i < count; i++)
            {
                var cluster = i % glyphs.Count;
                var glyph = glyphs[cluster];
                var center = distance + glyph.Advance / 2;
                if (!IsClipped(path, center, glyph, options))
                    results.Add(new FlowItem(cluster, center, path.Sample(center)));

                distance += glyph.Advance + spacing;
            }

            return spacing;
        }

        private static int GlyphCount(ILayoutPath path, IReadOnlyList<FlowGlyph> glyphs, double advanceSum,
            FlowOptions options)
        {
            if (options.Fill == FlowFill.None) return glyphs.Count;

            if (advanceSum + options.Tracking * glyphs.Count <= 0)
                throw new ArgumentException("Repeating needs glyphs that take up space.", nameof(glyphs));

            // A closed loop pays one extra gap at the seam; an open path does not.
            var gapBudget = path.IsClosed ? 0 : options.Tracking;
            var count = 0;
            var used = 0.0;
            while (count < MaxGlyphs)
            {
                var next = used + glyphs[count % glyphs.Count].Advance + options.Tracking;
                if (next > path.Length + gapBudget + Epsilon(path)) break;

                used = next;
                count++;
            }

            if (count >= MaxGlyphs)
                throw new ArgumentException($"Repeating exceeds {MaxGlyphs} glyphs.", nameof(glyphs));

            if (options.Fill == FlowFill.WholeText) count -= count % glyphs.Count;

            // Always show the text at least once, even if it overflows.
            return Math.Max(count, glyphs.Count);
        }

        private static double RepeatedAdvance(IReadOnlyList<FlowGlyph> glyphs, double advanceSum, int count)
        {
            var total = advanceSum * (count / glyphs.Count);
            for (var i = 0; i < count % glyphs.Count; i++)
                total += glyphs[i].Advance;

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

        // Judged by the ink (advance box minus bearings), so a flush glyph is never dropped for its blank margin.
        private static bool IsClipped(ILayoutPath path, double center, FlowGlyph glyph, FlowOptions options)
        {
            if (path.IsClosed || options.KeepOverflow) return false;

            var epsilon = Epsilon(path);
            var start = center - glyph.Advance / 2 + Math.Max(0, glyph.LeadingBearing);
            var end = center + glyph.Advance / 2 - Math.Max(0, glyph.TrailingBearing);
            return start < -epsilon || end > path.Length + epsilon;
        }

        private static double Epsilon(ILayoutPath path) => path.Length * 1e-9;
    }
}
