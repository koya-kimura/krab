using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Library
{
    /// <summary>One clip: a still (one frame) or a numbered sequence. Frame paths are relative to the library root.</summary>
    public sealed class ClipManifest
    {
        public ClipManifest(string name, IReadOnlyList<string> framePaths)
        {
            Name = Require.NotNull(name, nameof(name));
            FramePaths = Require.NotNull(framePaths, nameof(framePaths));
            if (framePaths.Count == 0) throw new ArgumentException("A clip needs at least one frame.", nameof(framePaths));
        }

        public string Name { get; }

        public IReadOnlyList<string> FramePaths { get; }

        public int FrameCount => FramePaths.Count;
    }
}
