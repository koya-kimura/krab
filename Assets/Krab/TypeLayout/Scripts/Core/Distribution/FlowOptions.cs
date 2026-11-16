namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>Every default (including <c>default(FlowOptions)</c>) is a valid setting.</summary>
    public readonly struct FlowOptions
    {
        public FlowOptions(double tracking = 0, FlowAlignment alignment = FlowAlignment.Start,
            FlowFill fill = FlowFill.None, double offset = 0, bool keepOverflow = false, bool inkFlush = false)
        {
            Tracking = Require.Finite(tracking, nameof(tracking));
            Alignment = alignment;
            Fill = fill;
            Offset = Require.Finite(offset, nameof(offset));
            KeepOverflow = keepOverflow;
            InkFlush = inkFlush;
        }

        /// <summary>
        /// Extra space between glyphs in output pixels. Replaced by the computed spacing when justified.
        /// </summary>
        public double Tracking { get; }

        public FlowAlignment Alignment { get; }

        public FlowFill Fill { get; }

        /// <summary>
        /// Shift along the path in output pixels, applied after alignment (scrolling). Closed paths wrap; repeated
        /// text on an open path scrolls as an endless band.
        /// </summary>
        public double Offset { get; }

        /// <summary>
        /// Open paths only: keeps glyphs whose advance box leaves the path. By default they are dropped.
        /// </summary>
        public bool KeepOverflow { get; }
    

        /// <summary>
        /// Open paths only: aligns the drawn shapes of the first and last glyph (not their advance boxes) to the
        /// path ends, so Start/End/Justify text sits exactly on a margin.
        /// </summary>
        public bool InkFlush { get; }
    }
}
