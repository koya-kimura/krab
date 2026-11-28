using System;
using Krab.ImageLayer.Core.Geometry;

namespace Krab.ImageLayer.Core.Placement
{
    /// <summary>
    /// Turns image() arguments into rectangles. Positions refer to the full source canvas, as if the untrimmed PNG
    /// were drawn; <see cref="MapContent"/> then gives the smaller quad that actually carries pixels.
    /// </summary>
    public static class ImagePlacement
    {
        /// <summary>Rectangle of the full source canvas for the four numbers of an image() call.</summary>
        public static DrawRect Resolve(ImageMode mode, double a, double b, double c, double d)
        {
            switch (mode)
            {
                case ImageMode.Corner:
                    return new DrawRect(a, b, c, d);
                case ImageMode.Center:
                    return new DrawRect(a - c / 2, b - d / 2, c, d);
                case ImageMode.Corners:
                    return DrawRect.FromEdges(Math.Min(a, c), Math.Min(b, d), Math.Max(a, c), Math.Max(b, d));
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        /// <summary>Quad covering only the opaque content when the full canvas is drawn at canvasRect.</summary>
        public static DrawRect MapContent(DrawRect canvasRect, TrimmedFrame frame)
        {
            double scaleX = canvasRect.Width / frame.SourceWidth;
            double scaleY = canvasRect.Height / frame.SourceHeight;
            var content = frame.Content;
            return new DrawRect(canvasRect.X + content.X * scaleX, canvasRect.Y + content.Y * scaleY,
                content.Width * scaleX, content.Height * scaleY);
        }

        /// <summary>
        /// Rectangle of the full source canvas that scales the reference area (in source pixels) into the box.
        /// Pass the full canvas to fit the image, or a clip's content union to fit what is actually drawn without
        /// the size changing from frame to frame. alignX / alignY place the scaled area: 0 = left/top,
        /// 0.5 = center, 1 = right/bottom.
        /// </summary>
        public static DrawRect Fit(
            DrawRect box, int sourceWidth, int sourceHeight, PixelRect reference, FitMode mode,
            double alignX = 0.5, double alignY = 0.5)
        {
            Require.Positive(sourceWidth, nameof(sourceWidth));
            Require.Positive(sourceHeight, nameof(sourceHeight));
            Require.Finite(alignX, nameof(alignX));
            Require.Finite(alignY, nameof(alignY));
            if (reference.IsEmpty) throw new ArgumentException("Reference area is empty.", nameof(reference));

            double scaleX = box.Width / reference.Width;
            double scaleY = box.Height / reference.Height;
            double scale = mode switch
            {
                FitMode.Contain => Math.Min(scaleX, scaleY),
                FitMode.Cover => Math.Max(scaleX, scaleY),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            double referenceX = box.X + (box.Width - reference.Width * scale) * alignX;
            double referenceY = box.Y + (box.Height - reference.Height * scale) * alignY;
            return new DrawRect(referenceX - reference.X * scale, referenceY - reference.Y * scale,
                sourceWidth * scale, sourceHeight * scale);
        }
    }
}
