namespace Krab.TypeLayout.Core.Text
{
    /// <summary>
    /// Metrics of one cluster at size 1 (multiply by the text size). Ink bounds are relative to the pen origin
    /// on the baseline, Y down, so ink above the baseline is negative.
    /// </summary>
    public readonly struct GlyphMeasure
    {
        public GlyphMeasure(double advance, double inkLeft, double inkTop, double inkRight, double inkBottom)
        {
            Advance = Require.NonNegative(advance, nameof(advance));
            InkLeft = Require.Finite(inkLeft, nameof(inkLeft));
            InkTop = Require.Finite(inkTop, nameof(inkTop));
            InkRight = Require.Finite(inkRight, nameof(inkRight));
            InkBottom = Require.Finite(inkBottom, nameof(inkBottom));
            HasInk = inkRight > inkLeft && inkBottom > inkTop;
        }

        public double Advance { get; }

        public double InkLeft { get; }

        public double InkTop { get; }

        public double InkRight { get; }

        public double InkBottom { get; }

        /// <summary>False for spaces and other clusters that draw nothing.</summary>
        public bool HasInk { get; }

        public static GlyphMeasure Blank(double advance) => new(advance, 0, 0, 0, 0);
    }
}
