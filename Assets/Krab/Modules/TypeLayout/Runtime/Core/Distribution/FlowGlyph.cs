using Krab.TypeLayout.Core.Text;

namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>
    /// Width of one glyph along the path in output pixels, with the blank margins inside its advance box.
    /// </summary>
    public readonly struct FlowGlyph
    {
        public FlowGlyph(double advance, double leadingBearing = 0, double trailingBearing = 0)
        {
            Advance = Require.NonNegative(advance, nameof(advance));
            LeadingBearing = Require.Finite(leadingBearing, nameof(leadingBearing));
            TrailingBearing = Require.Finite(trailingBearing, nameof(trailingBearing));
        }

        public double Advance { get; }

        /// <summary>Gap from the start of the advance box to the ink (negative when the ink overhangs).</summary>
        public double LeadingBearing { get; }

        /// <summary>Gap from the ink to the end of the advance box (negative when the ink overhangs).</summary>
        public double TrailingBearing { get; }

        /// <summary>Blank glyphs such as spaces keep no bearings, so their whole box counts.</summary>
        public static FlowGlyph FromMeasure(GlyphMeasure measure, double size)
        {
            Require.Positive(size, nameof(size));
            if (!measure.HasInk) return new FlowGlyph(measure.Advance * size);

            var trailing = (measure.Advance - measure.InkRight) * size;
            return new FlowGlyph(measure.Advance * size, measure.InkLeft * size, trailing);
        }
    }
}
