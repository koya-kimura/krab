using System;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Core.Paths
{
    /// <summary>
    /// Circular arc. A positive sweep turns clockwise on screen, which keeps glyph tops pointing outward;
    /// a negative sweep points them inward. A sweep of ±360 makes a closed circle.
    /// </summary>
    public sealed class ArcPath : ILayoutPath
    {
        private readonly Point2 _center;
        private readonly double _radius;
        private readonly double _startDegrees;
        private readonly double _sweepDegrees;

        public ArcPath(Point2 center, double radius, double startDegrees, double sweepDegrees)
        {
            _center = center;
            _radius = Require.Positive(radius, nameof(radius));
            _startDegrees = Require.Finite(startDegrees, nameof(startDegrees));
            _sweepDegrees = Require.Finite(sweepDegrees, nameof(sweepDegrees));
            if (_sweepDegrees == 0 || Math.Abs(_sweepDegrees) > 360)
            {
                throw new ArgumentOutOfRangeException(nameof(sweepDegrees), sweepDegrees,
                    "Sweep must be in [-360, 0) or (0, 360].");
            }

            Length = _radius * Angles.ToRadians(Math.Abs(_sweepDegrees));
            IsClosed = Math.Abs(_sweepDegrees) == 360;
        }

        public double Length { get; }

        public bool IsClosed { get; }

        /// <summary>Full circle starting at the top and running clockwise (glyphs upright at the top).</summary>
        public static ArcPath Circle(Point2 center, double radius, bool clockwise = true)
        {
            return new ArcPath(center, radius, -90, clockwise ? 360 : -360);
        }

        public LayoutSlot Sample(double distance)
        {
            Require.Finite(distance, nameof(distance));
            if (IsClosed) distance = PathMath.Wrap(distance, Length);

            var sign = Math.Sign(_sweepDegrees);
            var angle = _startDegrees + sign * Angles.ToDegrees(distance / _radius);
            var position = _center + Point2.FromAngle(angle) * _radius;
            return new LayoutSlot(position, Angles.Normalize(angle + sign * 90));
        }
    }
}
