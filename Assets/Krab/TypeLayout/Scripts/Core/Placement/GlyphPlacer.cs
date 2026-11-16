using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Text;

namespace Krab.TypeLayout.Core.Placement
{
    /// <summary>
    /// Turns slots and clusters into pen origins, so the renderer only translates, rotates and draws.
    /// </summary>
    public static class GlyphPlacer
    {
        public static GlyphPlacement Place(LayoutSlot slot, string text, IGlyphMetrics metrics, PlacementOptions options,
            int index = 0)
        {
            Require.NotNull(text, nameof(text));
            Require.NotNull(metrics, nameof(metrics));
            Require.Positive(options.Size, nameof(options));

            var measure = metrics.Measure(text);
            var rotation = options.Rotation == RotationMode.FollowPath ? slot.DirectionDegrees : 0;
            rotation = Angles.Normalize(rotation + options.RotationOffsetDegrees);

            var anchorOffset = new Point2(HorizontalOffset(measure, options.Horizontal),
                VerticalOffset(measure, metrics, options.Vertical)) * options.Size;
            var origin = slot.Position - anchorOffset.Rotate(rotation);
            if (options.SnapToPixel) origin = new Point2(Math.Round(origin.X), Math.Round(origin.Y));

            return new GlyphPlacement(text, origin, rotation, options.Size, slot.Position, index);
        }

        /// <summary>Places texts[i] at slots[i] into <paramref name="results"/>.</summary>
        public static void Place(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<string> texts, IGlyphMetrics metrics,
            PlacementOptions options, List<GlyphPlacement> results)
        {
            Require.NotNull(slots, nameof(slots));
            Require.NotNull(texts, nameof(texts));
            Require.NotNull(results, nameof(results));
            if (slots.Count != texts.Count)
                throw new ArgumentException("Each slot needs exactly one text.", nameof(texts));

            results.Clear();
            for (var i = 0; i < slots.Count; i++)
                results.Add(Place(slots[i], texts[i], metrics, options, i));
        }

        // Anchor position relative to the pen origin, at size 1, in the glyph's unrotated frame (Y down).
        private static double HorizontalOffset(GlyphMeasure measure, HorizontalAnchor anchor)
        {
            if (anchor == HorizontalAnchor.InkCenter && measure.HasInk) return (measure.InkLeft + measure.InkRight) / 2;

            return measure.Advance / 2;
        }

        private static double VerticalOffset(GlyphMeasure measure, IGlyphMetrics metrics, VerticalAnchor anchor)
        {
            return anchor switch
            {
                VerticalAnchor.Baseline => 0,
                VerticalAnchor.EmCenter => (metrics.Descent - metrics.Ascent) / 2,
                VerticalAnchor.CapCenter => -metrics.CapHeight / 2,
                VerticalAnchor.InkCenter when measure.HasInk => (measure.InkTop + measure.InkBottom) / 2,
                VerticalAnchor.InkCenter => -metrics.CapHeight / 2,
                _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
            };
        }
    }
}
