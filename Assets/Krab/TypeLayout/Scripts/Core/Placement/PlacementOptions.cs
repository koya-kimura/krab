namespace Krab.TypeLayout.Core.Placement
{
    public readonly struct PlacementOptions
    {
        public PlacementOptions(double size, HorizontalAnchor horizontal = HorizontalAnchor.AdvanceCenter,
            VerticalAnchor vertical = VerticalAnchor.CapCenter, RotationMode rotation = RotationMode.FollowPath,
            double rotationOffsetDegrees = 0, bool snapToPixel = false)
        {
            Size = Require.Positive(size, nameof(size));
            Horizontal = horizontal;
            Vertical = vertical;
            Rotation = rotation;
            RotationOffsetDegrees = Require.Finite(rotationOffsetDegrees, nameof(rotationOffsetDegrees));
            SnapToPixel = snapToPixel;
        }

        /// <summary>Text size (em) in output pixels.</summary>
        public double Size { get; }

        public HorizontalAnchor Horizontal { get; }

        public VerticalAnchor Vertical { get; }

        public RotationMode Rotation { get; }

        /// <summary>Added to the glyph rotation, turning each glyph around its anchor.</summary>
        public double RotationOffsetDegrees { get; }

        /// <summary>Rounds the pen origin to whole pixels. Exact only for glyphs at multiples of 90 degrees.</summary>
        public bool SnapToPixel { get; }
    }
}
