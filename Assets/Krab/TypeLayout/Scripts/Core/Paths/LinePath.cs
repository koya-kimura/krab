using System;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    public sealed class LinePath : ILayoutPath
    {
        private readonly Point2 _direction;
        private readonly double _directionDegrees;

        public LinePath(Point2 start, Point2 end)
        {
            var delta = end - start;
            Length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            if (Length <= 0) throw new ArgumentException("Line start and end must differ.", nameof(end));

            _direction = delta * (1 / Length);
            _directionDegrees = AxisAlignedDegrees(delta) ?? Angles.ToDegrees(Math.Atan2(delta.Y, delta.X));
            Start = start;
            End = end;
        }

        public Point2 Start { get; }

        public Point2 End { get; }

        public double Length { get; }

        public bool IsClosed => false;

        /// <summary>
        /// Runs along one side of the rectangle in clockwise reading order (the bottom reads right to left).
        /// </summary>
        public static LinePath AlongEdge(Rect2 rect, RectEdge edge)
        {
            return edge switch
            {
                RectEdge.Top => new LinePath(rect.PointAt(Anchor.TopLeft), rect.PointAt(Anchor.TopRight)),
                RectEdge.Right => new LinePath(rect.PointAt(Anchor.TopRight), rect.PointAt(Anchor.BottomRight)),
                RectEdge.Bottom => new LinePath(rect.PointAt(Anchor.BottomRight), rect.PointAt(Anchor.BottomLeft)),
                RectEdge.Left => new LinePath(rect.PointAt(Anchor.BottomLeft), rect.PointAt(Anchor.TopLeft)),
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null)
            };
        }

        public LayoutSlot Sample(double distance)
        {
            Require.Finite(distance, nameof(distance));
            if (distance == Length) return new LayoutSlot(End, _directionDegrees);

            return new LayoutSlot(Start + _direction * distance, _directionDegrees);
        }

        private static double? AxisAlignedDegrees(Point2 delta)
        {
            if (delta.Y == 0) return delta.X > 0 ? 0 : 180;
            if (delta.X == 0) return delta.Y > 0 ? 90 : -90;

            return null;
        }
    }
}
