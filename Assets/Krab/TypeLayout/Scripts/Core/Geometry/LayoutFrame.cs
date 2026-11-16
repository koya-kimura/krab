namespace Krab.TypeLayout.Core.Geometry
{
    /// <summary>
    /// Output canvas. Layout lengths are written in reference pixels (a canvas <see cref="ReferenceHeight"/> tall)
    /// and multiplied by <see cref="Unit"/>, so a composition keeps its proportions when the resolution changes.
    /// </summary>
    public readonly struct LayoutFrame
    {
        public const double DefaultReferenceHeight = 1080;

        public LayoutFrame(double width, double height, double referenceHeight = DefaultReferenceHeight)
        {
            Width = Require.Positive(width, nameof(width));
            Height = Require.Positive(height, nameof(height));
            ReferenceHeight = Require.Positive(referenceHeight, nameof(referenceHeight));
        }

        public double Width { get; }

        public double Height { get; }

        public double ReferenceHeight { get; }

        /// <summary>Output pixels per reference pixel.</summary>
        public double Unit => Height / ReferenceHeight;

        public Rect2 Bounds => new(0, 0, Width, Height);

        public double ToPixels(double referenceLength) => Require.Finite(referenceLength, nameof(referenceLength)) * Unit;

        /// <summary>Bounds inset by a margin given in reference pixels.</summary>
        public Rect2 SafeArea(double marginReference) => Bounds.Inset(ToPixels(marginReference));
    }
}
