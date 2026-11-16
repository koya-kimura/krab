using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Placement
{
    /// <summary>
    /// A glyph ready to draw: put the pen at <see cref="Origin"/>, rotate by <see cref="RotationDegrees"/>,
    /// and draw <see cref="Text"/> left- and baseline-aligned at <see cref="Size"/>.
    /// </summary>
    public readonly struct GlyphPlacement
    {
        public GlyphPlacement(string text, Point2 origin, double rotationDegrees, double size, Point2 anchor, int index)
        {
            Text = text;
            Origin = origin;
            RotationDegrees = rotationDegrees;
            Size = size;
            Anchor = anchor;
            Index = index;
        }

        public string Text { get; }

        /// <summary>Pen position on the baseline at the left of the advance box, in output pixels.</summary>
        public Point2 Origin { get; }

        public double RotationDegrees { get; }

        public double Size { get; }

        /// <summary>The slot position the glyph was anchored to (for guides).</summary>
        public Point2 Anchor { get; }

        /// <summary>Order of the glyph in its layout.</summary>
        public int Index { get; }
    }
}
