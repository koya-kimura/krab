using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>A point to place one glyph at, with the reading direction there (0 = rightward, clockwise positive).</summary>
    public readonly struct LayoutSlot
    {
        public LayoutSlot(Point2 position, double directionDegrees)
        {
            Position = position;
            DirectionDegrees = Require.Finite(directionDegrees, nameof(directionDegrees));
        }

        public Point2 Position { get; }

        public double DirectionDegrees { get; }
    }
}
