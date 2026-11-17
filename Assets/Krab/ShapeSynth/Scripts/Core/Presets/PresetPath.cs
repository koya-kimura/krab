using System.Linq;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>
    /// A preset name read as a "group/name" path. The first segment is the group (a Performance tab) and the rest
    /// is the title shown in that group. Blank segments and surrounding spaces are ignored, so "fx/a", " fx / a "
    /// and "/fx//a" are the same. A name without '/' has no group.
    /// </summary>
    public readonly struct PresetPath
    {
        public const char Separator = '/';

        /// <summary>The first segment, or empty when the name has a single segment.</summary>
        public string Group { get; }

        /// <summary>The remaining segments joined by '/'; never empty unless the name itself is blank.</summary>
        public string Title { get; }

        public bool HasGroup => Group.Length > 0;

        private PresetPath(string group, string title)
        {
            Group = group;
            Title = title;
        }

        public static PresetPath Parse(string name)
        {
            var segments = (name ?? string.Empty).Split(Separator)
                .Select(segment => segment.Trim())
                .Where(segment => segment.Length > 0)
                .ToArray();
            if (segments.Length == 0) return new PresetPath(string.Empty, string.Empty);
            if (segments.Length == 1) return new PresetPath(string.Empty, segments[0]);

            return new PresetPath(segments[0], string.Join(Separator.ToString(), segments, 1, segments.Length - 1));
        }

        /// <summary>The normalized full path, e.g. "fx/sample".</summary>
        public override string ToString() => HasGroup ? Group + Separator + Title : Title;
    }
}
