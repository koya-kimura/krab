using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Library
{
    /// <summary>
    /// Builds a library manifest from file paths relative to the library root ('/' separated). The folder layout is
    /// the definition: "set/name.png" is a still, "set/clip/frame.png" is a frame of a sequence clip.
    /// </summary>
    public static class ManifestBuilder
    {
        private const string PngExtension = ".png";
        private const string MetaExtension = ".meta";

        public static LibraryManifest Build(string libraryName, IEnumerable<string> relativePaths)
        {
            Require.NotNull(libraryName, nameof(libraryName));
            Require.NotNull(relativePaths, nameof(relativePaths));

            var issues = new List<ManifestIssue>();
            var folders = CollectFolders(relativePaths, issues);

            var setNames = new List<string>(folders.Keys);
            setNames.Sort(NaturalOrder.Instance);
            var sets = new List<SetManifest>();
            foreach (var setName in setNames)
            {
                var set = BuildSet(setName, folders[setName], issues);
                if (set != null) sets.Add(set);
            }

            return new LibraryManifest(libraryName, sets, issues);
        }

        private static Dictionary<string, SetFolder> CollectFolders(
            IEnumerable<string> relativePaths, List<ManifestIssue> issues)
        {
            var folders = new Dictionary<string, SetFolder>(StringComparer.Ordinal);
            foreach (var rawPath in relativePaths)
            {
                if (string.IsNullOrEmpty(rawPath)) continue;

                string path = rawPath.Replace('\\', '/').Trim('/');
                var parts = path.Split('/');
                string fileName = parts[parts.Length - 1];
                if (IsIgnored(parts)) continue;

                if (!fileName.EndsWith(PngExtension, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ManifestIssue(IssueSeverity.Warning, path, "Not a PNG; ignored."));
                    continue;
                }

                if (parts.Length < 2 || parts.Length > 3)
                {
                    issues.Add(new ManifestIssue(IssueSeverity.Warning, path,
                        "PNGs belong in set/ (stills) or set/clip/ (sequences); ignored."));
                    continue;
                }

                if (!folders.TryGetValue(parts[0], out var folder))
                {
                    folder = new SetFolder();
                    folders.Add(parts[0], folder);
                }

                if (parts.Length == 2)
                {
                    folder.Stills.Add(path);
                    continue;
                }

                if (!folder.Clips.TryGetValue(parts[1], out var frames))
                {
                    frames = new List<string>();
                    folder.Clips.Add(parts[1], frames);
                }

                frames.Add(path);
            }

            return folders;
        }

        // Hidden files (".DS_Store") and Unity's .meta files are expected next to the images.
        private static bool IsIgnored(string[] parts)
        {
            foreach (var part in parts)
            {
                if (part.StartsWith(".", StringComparison.Ordinal)) return true;
            }

            return parts[parts.Length - 1].EndsWith(MetaExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static SetManifest BuildSet(string setName, SetFolder folder, List<ManifestIssue> issues)
        {
            if (folder.Stills.Count > 0 && folder.Clips.Count > 0)
            {
                issues.Add(new ManifestIssue(IssueSeverity.Error, setName,
                    "Set mixes PNGs and clip folders; skipped. Use either stills or sequences in one set."));
                return null;
            }

            var clips = new List<ClipManifest>();
            if (folder.Stills.Count > 0)
            {
                folder.Stills.Sort(CompareFileNames);
                foreach (var path in folder.Stills)
                    clips.Add(new ClipManifest(NameWithoutExtension(path), new[] { path }));

                return new SetManifest(setName, SetKind.Stills, clips);
            }

            var clipNames = new List<string>(folder.Clips.Keys);
            clipNames.Sort(NaturalOrder.Instance);
            foreach (var clipName in clipNames)
            {
                var frames = folder.Clips[clipName];
                frames.Sort(CompareFileNames);
                CheckNumbering(setName + "/" + clipName, frames, issues);
                clips.Add(new ClipManifest(clipName, frames));
            }

            return new SetManifest(setName, SetKind.Sequences, clips);
        }

        // Frame numbers that skip or repeat usually mean a missing or duplicated export.
        private static void CheckNumbering(string clipPath, List<string> frames, List<ManifestIssue> issues)
        {
            long previous = 0;
            for (int index = 0; index < frames.Count; index++)
            {
                if (!long.TryParse(NameWithoutExtension(frames[index]), out long number)) return;

                if (index > 0 && number == previous)
                {
                    issues.Add(new ManifestIssue(IssueSeverity.Warning, clipPath,
                        $"Frame number {number} appears more than once."));
                    return;
                }

                if (index > 0 && number != previous + 1)
                {
                    issues.Add(new ManifestIssue(IssueSeverity.Warning, clipPath,
                        $"Frame numbers skip from {previous} to {number}."));
                    return;
                }

                previous = number;
            }
        }

        private static int CompareFileNames(string x, string y)
        {
            return NaturalOrder.Instance.Compare(FileName(x), FileName(y));
        }

        private static string FileName(string path) => path.Substring(path.LastIndexOf('/') + 1);

        private static string NameWithoutExtension(string path)
        {
            string fileName = FileName(path);
            return fileName.Substring(0, fileName.Length - PngExtension.Length);
        }

        private sealed class SetFolder
        {
            public List<string> Stills { get; } = new();

            public Dictionary<string, List<string>> Clips { get; } = new(StringComparer.Ordinal);
        }
    }
}
