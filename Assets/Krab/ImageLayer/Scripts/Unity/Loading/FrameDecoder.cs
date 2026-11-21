using System;
using System.IO;
using System.Runtime.InteropServices;
using Krab.ImageLayer.Core.Geometry;
using Krab.ImageLayer.Unity.Library;
using UnityEngine;

namespace Krab.ImageLayer.Unity.Loading
{
    /// <summary>
    /// Decodes one PNG into a trimmed, premultiplied texture. Premultiplying keeps the transparent margin from
    /// bleeding dark fringes into scaled edges.
    /// </summary>
    internal static class FrameDecoder
    {
        // Block compression works on 4×4 tiles.
        private const int BlockSize = 4;

        public static ImageFrame Decode(string path, in ImageLoadOptions options, out long textureBytes)
        {
            textureBytes = 0;
            var bytes = File.ReadAllBytes(path);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(bytes, false)) throw new InvalidDataException("Not a readable PNG.");

                var pixels = source.GetPixels32();
                int width = source.width;
                int height = source.height;
                var rgba = MemoryMarshal.AsBytes(pixels.AsSpan());
                if (!AlphaBounds.TryFind(rgba, width, height, true, out var content, options.AlphaThreshold))
                    return new ImageFrame(null, new TrimmedFrame(width, height, default));

                var texture = CreateTrimmedTexture(pixels, width, height, content, options);
                textureBytes = EstimateBytes(texture, options);
                return new ImageFrame(texture, new TrimmedFrame(width, height, content));
            }
            finally
            {
                UnityObjects.Destroy(source);
            }
        }

        private static Texture2D CreateTrimmedTexture(
            Color32[] pixels, int width, int height, PixelRect content, in ImageLoadOptions options)
        {
            int paddedWidth = RoundUp(content.Width);
            int paddedHeight = RoundUp(content.Height);
            var trimmed = new Color32[paddedWidth * paddedHeight];

            // Content is in top-left coordinates; Unity stores the bottom row first.
            int sourceRowStart = height - content.Bottom;
            for (int row = 0; row < content.Height; row++)
            {
                int sourceIndex = (sourceRowStart + row) * width + content.X;
                int targetIndex = row * paddedWidth;
                for (int x = 0; x < content.Width; x++)
                    trimmed[targetIndex + x] = Premultiply(pixels[sourceIndex + x]);
            }

            var texture = new Texture2D(paddedWidth, paddedHeight, TextureFormat.RGBA32, options.Mipmaps)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(trimmed);
            texture.Apply(options.Mipmaps, !options.Compress);
            if (options.Compress)
            {
                texture.Compress(false);
                texture.Apply(false, true);
            }

            return texture;
        }

        private static Color32 Premultiply(Color32 color)
        {
            if (color.a == 255) return color;

            int alpha = color.a;
            return new Color32((byte)((color.r * alpha + 127) / 255), (byte)((color.g * alpha + 127) / 255),
                (byte)((color.b * alpha + 127) / 255), color.a);
        }

        private static int RoundUp(int size) => (size + BlockSize - 1) / BlockSize * BlockSize;

        private static long EstimateBytes(Texture2D texture, in ImageLoadOptions options)
        {
            long pixelCount = (long)texture.width * texture.height;
            long baseBytes = options.Compress ? pixelCount : pixelCount * 4;
            return options.Mipmaps ? baseBytes * 4 / 3 : baseBytes;
        }
    }
}
