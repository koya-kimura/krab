using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Library
{
    /// <summary>A named group of clips. Stills and sequences share the same clip shape.</summary>
    public sealed class SetManifest
    {
        public SetManifest(string name, SetKind kind, IReadOnlyList<ClipManifest> clips)
        {
            Name = Require.NotNull(name, nameof(name));
            Kind = kind;
            Clips = Require.NotNull(clips, nameof(clips));
            if (clips.Count == 0) throw new ArgumentException("A set needs at least one clip.", nameof(clips));
        }

        public string Name { get; }

        public SetKind Kind { get; }

        public IReadOnlyList<ClipManifest> Clips { get; }

        public int ClipCount => Clips.Count;

        public ClipManifest this[int index] => Clips[index];
    }
}
