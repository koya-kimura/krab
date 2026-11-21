using System;

namespace Krab.ImageLayer.Core.Geometry
{
    /// <summary>Finds the opaque part of an RGBA32 image so the transparent margin can be cut away.</summary>
    public static class AlphaBounds
    {
        private const int BytesPerPixel = 4;
        private const int AlphaOffset = 3;

        /// <summary>
        /// Bounds of pixels whose alpha is above the threshold, in top-left coordinates. Set rowsBottomUp for
        /// Unity's pixel order (first row = bottom). Returns false when every pixel is at or below the threshold.
        /// </summary>
        public static bool TryFind(
            ReadOnlySpan<byte> rgba32, int width, int height, bool rowsBottomUp, out PixelRect bounds,
            byte threshold = 0)
        {
            Require.Positive(width, nameof(width));
            Require.Positive(height, nameof(height));
            if (rgba32.Length != width * height * BytesPerPixel)
                throw new ArgumentException("Pixel data does not match width × height × 4.", nameof(rgba32));

            int left = width;
            int right = -1;
            int top = -1;
            int bottom = -1;
            for (int row = 0; row < height; row++)
            {
                var rowBytes = rgba32.Slice(row * width * BytesPerPixel, width * BytesPerPixel);
                int first = FirstOpaque(rowBytes, width, threshold);
                if (first < 0) continue;

                int last = LastOpaque(rowBytes, width, threshold);
                left = Math.Min(left, first);
                right = Math.Max(right, last);
                if (top < 0) top = row;
                bottom = row;
            }

            if (right < 0)
            {
                bounds = default;
                return false;
            }

            if (rowsBottomUp)
            {
                int flippedTop = height - 1 - bottom;
                bottom = height - 1 - top;
                top = flippedTop;
            }

            bounds = PixelRect.FromEdges(left, top, right + 1, bottom + 1);
            return true;
        }

        private static int FirstOpaque(ReadOnlySpan<byte> row, int width, byte threshold)
        {
            for (int x = 0; x < width; x++)
            {
                if (row[x * BytesPerPixel + AlphaOffset] > threshold) return x;
            }

            return -1;
        }

        private static int LastOpaque(ReadOnlySpan<byte> row, int width, byte threshold)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                if (row[x * BytesPerPixel + AlphaOffset] > threshold) return x;
            }

            return -1;
        }
    }
}
