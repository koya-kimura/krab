using System;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>
    /// Closed path around a rectangle, starting at a corner. Clockwise keeps glyph tops pointing outward,
    /// so the bottom edge reads upside down and the side edges read sideways. A corner belongs to the edge
    /// that starts there. A corner gap stops every edge short of the corners, so text bands running along
    /// two edges never collide; the path jumps across the gap.
    /// </summary>
    public sealed class RectPerimeterPath : ILayoutPath
    {
        private readonly LinePath[] _edges = new LinePath[4];
        private readonly double[] _edgeStarts = new double[4];

        /// <param name="cornerGap">Distance in output pixels each edge stops before a corner.</param>
        public RectPerimeterPath(Rect2 rect, Anchor startCorner = Anchor.TopLeft, bool clockwise = true,
            double cornerGap = 0)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                throw new ArgumentException("Rectangle must have a positive area.", nameof(rect));

            Require.NonNegative(cornerGap, nameof(cornerGap));
            if (cornerGap * 2 >= Math.Min(rect.Width, rect.Height))
                throw new ArgumentOutOfRangeException(nameof(cornerGap), cornerGap, "Gaps would remove an edge.");

            var corners = Corners(rect, startCorner, clockwise);
            var distance = 0.0;
            for (var i = 0; i < 4; i++)
            {
                var from = corners[i];
                var to = corners[(i + 1) % 4];
                var direction = (to - from) * (1 / Distance(from, to));
                _edges[i] = new LinePath(from + direction * cornerGap, to - direction * cornerGap);
                _edgeStarts[i] = distance;
                distance += _edges[i].Length;
            }

            Length = distance;
            Rect = rect;
            CornerGap = cornerGap;
        }

        public Rect2 Rect { get; }

        public double CornerGap { get; }

        public double Length { get; }

        public bool IsClosed => true;

        public LayoutSlot Sample(double distance)
        {
            Require.Finite(distance, nameof(distance));
            distance = PathMath.Wrap(distance, Length);

            for (var i = 3; i >= 0; i--)
            {
                if (distance >= _edgeStarts[i]) return _edges[i].Sample(distance - _edgeStarts[i]);
            }

            return _edges[0].Sample(0);
        }

        private static double Distance(Point2 a, Point2 b)
        {
            var delta = b - a;
            return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        }

        private static Point2[] Corners(Rect2 rect, Anchor startCorner, bool clockwise)
        {
            Anchor[] order = { Anchor.TopLeft, Anchor.TopRight, Anchor.BottomRight, Anchor.BottomLeft };
            var start = Array.IndexOf(order, startCorner);
            if (start < 0) throw new ArgumentOutOfRangeException(nameof(startCorner), startCorner, "Must be a corner.");

            var corners = new Point2[4];
            for (var i = 0; i < 4; i++)
            {
                var index = clockwise ? start + i : start - i;
                corners[i] = rect.PointAt(order[(index + 4) % 4]);
            }

            return corners;
        }
    }
}
