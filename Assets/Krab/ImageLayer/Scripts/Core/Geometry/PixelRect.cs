using System;

namespace Krab.ImageLayer.Core.Geometry
{
    /// <summary>Rectangle in source image pixels, top-left origin (Y down). Width and height are never negative.</summary>
    public readonly struct PixelRect : IEquatable<PixelRect>
    {
        public PixelRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = Require.NonNegative(width, nameof(width));
            Height = Require.NonNegative(height, nameof(height));
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public int Right => X + Width;

        public int Bottom => Y + Height;

        public bool IsEmpty => Width == 0 || Height == 0;

        public static PixelRect FromEdges(int left, int top, int right, int bottom)
        {
            return new PixelRect(left, top, right - left, bottom - top);
        }

        /// <summary>Smallest rectangle containing both. An empty rectangle contributes nothing.</summary>
        public PixelRect Union(PixelRect other)
        {
            if (IsEmpty) return other;
            if (other.IsEmpty) return this;

            return FromEdges(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Max(Right, other.Right),
                Math.Max(Bottom, other.Bottom));
        }

        public bool Equals(PixelRect other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj) => obj is PixelRect other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public override string ToString() => $"(x:{X}, y:{Y}, w:{Width}, h:{Height})";
    }
}
