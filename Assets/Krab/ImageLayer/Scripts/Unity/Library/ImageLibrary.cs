using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Library;

namespace Krab.ImageLayer.Unity.Library
{
    /// <summary>
    /// Loaded sets of one library folder. Owns every texture; <see cref="Dispose"/> destroys them.
    /// </summary>
    public sealed class ImageLibrary : IDisposable
    {
        private readonly Dictionary<string, ImageSet> _setsByName = new(StringComparer.Ordinal);

        public ImageLibrary(string name, IReadOnlyList<ImageSet> sets, IReadOnlyList<ManifestIssue> issues,
            long textureBytes, double loadSeconds)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Sets = sets ?? throw new ArgumentNullException(nameof(sets));
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            TextureBytes = textureBytes;
            LoadSeconds = loadSeconds;
            foreach (var set in sets)
                _setsByName.Add(set.Name, set);
        }

        public string Name { get; }

        public IReadOnlyList<ImageSet> Sets { get; }

        /// <summary>Problems found in the folder layout and while decoding. Empty when everything loaded.</summary>
        public IReadOnlyList<ManifestIssue> Issues { get; }

        /// <summary>GPU memory of all frame textures.</summary>
        public long TextureBytes { get; }

        public double LoadSeconds { get; }

        public bool IsDisposed { get; private set; }

        public bool TryGet(string setName, out ImageSet set)
        {
            set = null;
            return setName != null && _setsByName.TryGetValue(setName, out set);
        }

        /// <summary>Set by exact name. Throws when missing, so typos surface while a pattern is set up.</summary>
        public ImageSet Get(string setName)
        {
            if (TryGet(setName, out var set)) return set;

            throw new KeyNotFoundException(
                $"Image set '{setName}' is not in library '{Name}'. Sets: {string.Join(", ", _setsByName.Keys)}");
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            foreach (var set in Sets)
            foreach (var clip in set.Clips)
            foreach (var frame in clip.Frames)
                frame.Release();
        }
    }
}
