using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Library;

namespace Krab.ImageLayer.Unity.Library
{
    /// <summary>A named group of clips, as laid out in one set folder.</summary>
    public sealed class ImageSet
    {
        public ImageSet(string name, SetKind kind, IReadOnlyList<ImageClip> clips)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
            Clips = clips ?? throw new ArgumentNullException(nameof(clips));
            if (clips.Count == 0) throw new ArgumentException("A set needs at least one clip.", nameof(clips));
        }

        public string Name { get; }

        public SetKind Kind { get; }

        public IReadOnlyList<ImageClip> Clips { get; }

        public int ClipCount => Clips.Count;

        public ImageClip this[int index] => Clips[index];

        /// <summary>Clip by index, wrapping in both directions so any counter or random number picks a clip.</summary>
        public ImageClip Wrap(int index)
        {
            int count = Clips.Count;
            return Clips[((index % count) + count) % count];
        }
    }
}
