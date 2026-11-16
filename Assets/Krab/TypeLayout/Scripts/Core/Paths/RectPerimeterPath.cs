using System;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>
    /// Closed path around a rectangle, starting at a corner. Clockwise keeps glyph tops pointing outward,
    /// so the bottom edge reads upside down and the side edges read sideways. A corner belongs to the edge
    /// that starts there.
    /// </summary>
    public sealed class RectPerimeterPath : ILayoutPath
    {
        private readonly LinePath[] _edges = new LinePath[4];
        private readonly double[] _edgeStarts = new double[4];

        public RectPerimeterPath(Rect2 rect, Anchor startCorner = Anchor.TopLeft, bool clockwise = true)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                throw new ArgumentException("Rectangle must have a positive area.", nameof(rect));

            var corners = Corners(rect, startCorner, clockwise);
            var distance = 0.0;
            for (var i = 0; i < 4; i++)
            {
                _edges[i] = new LinePath(corners[i], corners[(i + 1) % 4]);
                _edgeStarts[i] = distance;
                distance += _edges[i].Length;
            }

            Length = distance;
            Rect = rect;
        }

        public Rect2 Rect { get; }

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
