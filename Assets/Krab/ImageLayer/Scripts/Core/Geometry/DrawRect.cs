using System;

namespace Krab.ImageLayer.Core.Geometry
{
    /// <summary>Rectangle in output pixels, top-left origin (Y down). Width and height are never negative.</summary>
    public readonly struct DrawRect : IEquatable<DrawRect>
    {
        public DrawRect(double x, double y, double width, double height)
        {
            X = Require.Finite(x, nameof(x));
            Y = Require.Finite(y, nameof(y));
            Width = Require.NonNegative(width, nameof(width));
            Height = Require.NonNegative(height, nameof(height));
        }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public double CenterX => X + Width / 2;

        public double CenterY => Y + Height / 2;

        public static DrawRect FromEdges(double left, double top, double right, double bottom)
        {
            return new DrawRect(left, top, right - left, bottom - top);
        }

        public bool Equals(DrawRect other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj) => obj is DrawRect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"(x:{X}, y:{Y}, w:{Width}, h:{Height})";
    }
}
