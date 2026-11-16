using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>
    /// Straight segments through the given points, for paths computed by script (waves, zigzags, hand-made shapes).
    /// Repeated consecutive points are skipped. A vertex belongs to the segment that starts there.
    /// </summary>
    public sealed class PolylinePath : ILayoutPath
    {
        private readonly LinePath[] _segments;
        private readonly double[] _segmentStarts;

        public PolylinePath(IReadOnlyList<Point2> points, bool closed = false)
        {
            Require.NotNull(points, nameof(points));
            var segments = new List<LinePath>(points.Count);
            var count = closed ? points.Count : points.Count - 1;
            for (var i = 0; i < count; i++)
            {
                var from = points[i];
                var to = points[(i + 1) % points.Count];
                if (!from.Equals(to)) segments.Add(new LinePath(from, to));
            }

            if (segments.Count == 0) throw new ArgumentException("Polyline needs two distinct points.", nameof(points));

            _segments = segments.ToArray();
            _segmentStarts = new double[_segments.Length];
            var distance = 0.0;
            for (var i = 0; i < _segments.Length; i++)
            {
                _segmentStarts[i] = distance;
                distance += _segments[i].Length;
            }

            Length = distance;
            IsClosed = closed;
        }

        public double Length { get; }

        public bool IsClosed { get; }

        public LayoutSlot Sample(double distance)
        {
            Require.Finite(distance, nameof(distance));
            if (IsClosed) distance = PathMath.Wrap(distance, Length);

            // Open paths extend along the first and last segments.
            if (distance < 0) return _segments[0].Sample(distance);

            var index = Array.BinarySearch(_segmentStarts, distance);
            if (index < 0) index = ~index - 1;

            return _segments[index].Sample(distance - _segmentStarts[index]);
        }
    }
}
