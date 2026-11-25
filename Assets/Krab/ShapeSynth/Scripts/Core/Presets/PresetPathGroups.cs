using System;
using System.Collections.Generic;
using System.Linq;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Items whose names are "group/name" paths, grouped the way the Performance tabs show presets.</summary>
    public static class PresetPathGroups
    {
        /// <summary>
        /// Named groups in ordinal order, then the ungrouped items (empty Group) last. Each group lists its items by
        /// Title; items with the same Title keep their given order.
        /// </summary>
        public static IReadOnlyList<(string Group, IReadOnlyList<(PresetPath Path, T Item)> Entries)> Of<T>(
            IEnumerable<T> items, Func<T, string> name)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (name == null) throw new ArgumentNullException(nameof(name));

            return items
                .Select(item => (Path: PresetPath.Parse(name(item)), Item: item))
                .GroupBy(entry => entry.Path.Group)
                .OrderBy(group => group.Key.Length == 0)
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => (group.Key, (IReadOnlyList<(PresetPath, T)>)group
                    .OrderBy(entry => entry.Path.Title, StringComparer.Ordinal)
                    .ToList()))
                .ToList();
        }
    }
}
