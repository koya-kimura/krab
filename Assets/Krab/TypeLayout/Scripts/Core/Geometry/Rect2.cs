using System;

namespace Krab.TypeLayout.Core.Geometry
{
    /// <summary>Axis-aligned rectangle in output pixels (Y down). Width and height are never negative.</summary>
    public readonly struct Rect2 : IEquatable<Rect2>
    {
        public Rect2(double x, double y, double width, double height)
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

        public double Left => X;

        public double Top => Y;

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public Point2 Center => new(X + Width / 2, Y + Height / 2);

        public static Rect2 FromEdges(double left, double top, double right, double bottom)
        {
            return new Rect2(left, top, right - left, bottom - top);
        }

        /// <summary>Shrinks each side; throws when the insets are larger than the rectangle.</summary>
        public Rect2 Inset(double left, double top, double right, double bottom)
        {
            Require.Finite(left, nameof(left));
            Require.Finite(top, nameof(top));
            Require.Finite(right, nameof(right));
            Require.Finite(bottom, nameof(bottom));
            if (left + right > Width || top + bottom > Height)
                throw new ArgumentOutOfRangeException(nameof(left), "Insets exceed the rectangle.");

            return FromEdges(Left + left, Top + top, Right - right, Bottom - bottom);
        }

        public Rect2 Inset(double all) => Inset(all, all, all, all);

        public Point2 PointAt(Anchor anchor)
        {
            var (u, v) = Fraction(anchor);
            return new Point2(X + Width * u, Y + Height * v);
        }

        /// <summary>Places a box of the given size inside this rectangle, aligned to the anchor.</summary>
        public Rect2 Place(double width, double height, Anchor anchor)
        {
            Require.NonNegative(width, nameof(width));
            Require.NonNegative(height, nameof(height));
            var (u, v) = Fraction(anchor);
            return new Rect2(X + (Width - width) * u, Y + (Height - height) * v, width, height);
        }

        public bool Equals(Rect2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object obj) => obj is Rect2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"(x:{X}, y:{Y}, w:{Width}, h:{Height})";

        private static (double U, double V) Fraction(Anchor anchor)
        {
            return anchor switch
            {
                Anchor.TopLeft => (0, 0),
                Anchor.Top => (0.5, 0),
                Anchor.TopRight => (1, 0),
                Anchor.Left => (0, 0.5),
                Anchor.Center => (0.5, 0.5),
                Anchor.Right => (1, 0.5),
                Anchor.BottomLeft => (0, 1),
                Anchor.Bottom => (0.5, 1),
                Anchor.BottomRight => (1, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
            };
        }
    }
}
