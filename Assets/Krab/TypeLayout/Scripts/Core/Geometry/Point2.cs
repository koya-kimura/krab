using System;

namespace Krab.TypeLayout.Core.Geometry
{
    /// <summary>Point in output pixels: origin at the top-left, X right, Y down.</summary>
    public readonly struct Point2 : IEquatable<Point2>
    {
        public Point2(double x, double y)
        {
            X = Require.Finite(x, nameof(x));
            Y = Require.Finite(y, nameof(y));
        }

        public double X { get; }

        public double Y { get; }

        public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);

        public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);

        public static Point2 operator *(Point2 point, double scale) => new(point.X * scale, point.Y * scale);

        public static Point2 Lerp(Point2 a, Point2 b, double t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        /// <summary>Unit vector at the angle; positive degrees turn clockwise on screen (Y down).</summary>
        public static Point2 FromAngle(double degrees)
        {
            var (cos, sin) = Angles.CosSin(degrees);
            return new Point2(cos, sin);
        }

        /// <summary>Rotates around the origin; positive degrees turn clockwise on screen.</summary>
        public Point2 Rotate(double degrees)
        {
            var (cos, sin) = Angles.CosSin(degrees);
            return new Point2(X * cos - Y * sin, X * sin + Y * cos);
        }

        public bool Equals(Point2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Point2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
