using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Frames;
using Krab.ImageLayer.Core.Geometry;

namespace Krab.ImageLayer.Unity.Library
{
    /// <summary>A still (one frame) or a numbered sequence. Frames share the size of the first frame.</summary>
    public sealed class ImageClip
    {
        public ImageClip(string name, IReadOnlyList<ImageFrame> frames)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Frames = frames ?? throw new ArgumentNullException(nameof(frames));
            if (frames.Count == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(frames));

            SourceWidth = frames[0].SourceWidth;
            SourceHeight = frames[0].SourceHeight;
            foreach (var frame in frames)
                ContentBounds = ContentBounds.Union(frame.Trim.Content);

            // A clip with only transparent frames still needs a reference area for fitting.
            if (ContentBounds.IsEmpty) ContentBounds = new PixelRect(0, 0, SourceWidth, SourceHeight);
        }

        public string Name { get; }

        public IReadOnlyList<ImageFrame> Frames { get; }

        public int FrameCount => Frames.Count;

        public int SourceWidth { get; }

        public int SourceHeight { get; }

        /// <summary>Union of every frame's content, in source pixels. Fitting to it keeps the size steady.</summary>
        public PixelRect ContentBounds { get; }

        public ImageFrame this[int index] => Frames[index];

        /// <summary>Frame for a progress where 1 is one pass through the clip.</summary>
        public ImageFrame AtProgress(double progress, FrameWrap wrap = FrameWrap.Loop)
        {
            return Frames[FrameSelector.FromProgress(progress, FrameCount, wrap)];
        }

        /// <summary>Frame for a position in frames, e.g. beats × frames per beat.</summary>
        public ImageFrame AtPosition(double position, FrameWrap wrap = FrameWrap.Loop)
        {
            return Frames[FrameSelector.FromPosition(position, FrameCount, wrap)];
        }
    }
}
