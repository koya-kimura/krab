using System;

namespace Krab.ImageLayer.Core.Geometry
{
    /// <summary>
    /// A frame whose transparent margin was cut away. Keeps the original canvas size so drawing the content at
    /// <see cref="Content"/> looks the same as drawing the full, untrimmed PNG.
    /// </summary>
    public readonly struct TrimmedFrame
    {
        public TrimmedFrame(int sourceWidth, int sourceHeight, PixelRect content)
        {
            SourceWidth = Require.Positive(sourceWidth, nameof(sourceWidth));
            SourceHeight = Require.Positive(sourceHeight, nameof(sourceHeight));
            if (!content.IsEmpty && (content.X < 0 || content.Y < 0 || content.Right > sourceWidth ||
                                     content.Bottom > sourceHeight))
                throw new ArgumentOutOfRangeException(nameof(content), content, "Content lies outside the source.");

            Content = content;
        }

        public int SourceWidth { get; }

        public int SourceHeight { get; }

        /// <summary>Opaque part in source pixels. Empty for a fully transparent frame, which draws nothing.</summary>
        public PixelRect Content { get; }

        public bool IsEmpty => Content.IsEmpty;
    }
}
