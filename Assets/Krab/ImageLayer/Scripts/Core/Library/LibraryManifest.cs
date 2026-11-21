using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Library
{
    /// <summary>The sets found in one library folder, in natural order, and the problems found while reading it.</summary>
    public sealed class LibraryManifest
    {
        private readonly Dictionary<string, SetManifest> _setsByName = new(StringComparer.Ordinal);

        public LibraryManifest(string name, IReadOnlyList<SetManifest> sets, IReadOnlyList<ManifestIssue> issues)
        {
            Name = Require.NotNull(name, nameof(name));
            Sets = Require.NotNull(sets, nameof(sets));
            Issues = Require.NotNull(issues, nameof(issues));
            foreach (var set in sets)
            {
                if (!_setsByName.TryAdd(set.Name, set))
                    throw new ArgumentException($"Duplicate set name '{set.Name}'.", nameof(sets));
            }
        }

        public string Name { get; }

        public IReadOnlyList<SetManifest> Sets { get; }

        public IReadOnlyList<ManifestIssue> Issues { get; }

        public int FrameCount
        {
            get
            {
                int count = 0;
                foreach (var set in Sets)
                foreach (var clip in set.Clips)
                    count += clip.FrameCount;

                return count;
            }
        }

        public bool TryGetSet(string name, out SetManifest set) => _setsByName.TryGetValue(name, out set);
    }
}
