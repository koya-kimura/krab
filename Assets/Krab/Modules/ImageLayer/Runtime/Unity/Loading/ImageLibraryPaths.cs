using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Krab.ImageLayer.Unity.Loading
{
    /// <summary>Locates image libraries: one folder per library under StreamingAssets/Krab/ImageLayer/Libraries.</summary>
    public static class ImageLibraryPaths
    {
        public const string RelativeRoot = "Krab/ImageLayer/Libraries";

        public static string Root => Path.Combine(Application.streamingAssetsPath, RelativeRoot);

        public static string LibraryFolder(string libraryName) => Path.Combine(Root, libraryName);

        /// <summary>Library folder names in natural order. Empty when the root folder does not exist.</summary>
        public static IReadOnlyList<string> ListLibraries()
        {
            var names = new List<string>();
            if (!Directory.Exists(Root)) return names;

            foreach (var directory in Directory.GetDirectories(Root))
            {
                string name = Path.GetFileName(directory);
                if (!name.StartsWith(".", StringComparison.Ordinal)) names.Add(name);
            }

            names.Sort(Core.Library.NaturalOrder.Instance);
            return names;
        }

        /// <summary>Every file below the library folder, relative to it with '/' separators.</summary>
        public static List<string> ListFiles(string libraryFolder)
        {
            var files = new List<string>();
            int prefixLength = libraryFolder.TrimEnd('/', '\\').Length + 1;
            foreach (var path in Directory.EnumerateFiles(libraryFolder, "*", SearchOption.AllDirectories))
                files.Add(path.Substring(prefixLength).Replace('\\', '/'));

            return files;
        }
    }
}
