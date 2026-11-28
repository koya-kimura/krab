using System;
using System.Collections.Generic;
using System.Text;

namespace Krab.ImageLayer.Core.Presets
{
    /// <summary>Ids and display names that do not collide with the ones already in a list.</summary>
    public static class PresetNaming
    {
        private const string FallbackId = "item";

        /// <summary>Lowercase ASCII letters, digits and '-' from the name; numbered when taken ("march-2").</summary>
        public static string CreateId(string name, ICollection<string> existingIds)
        {
            Require.NotNull(existingIds, nameof(existingIds));

            var baseId = Slug(name);
            if (!existingIds.Contains(baseId)) return baseId;

            for (var number = 2; ; number++)
            {
                var id = $"{baseId}-{number}";
                if (!existingIds.Contains(id)) return id;
            }
        }

        /// <summary>The name itself when free, otherwise numbered ("March 2").</summary>
        public static string CreateName(string name, ICollection<string> existingNames)
        {
            Require.NotNull(existingNames, nameof(existingNames));

            var baseName = string.IsNullOrWhiteSpace(name) ? FallbackId : name.Trim();
            if (!existingNames.Contains(baseName)) return baseName;

            for (var number = 2; ; number++)
            {
                var candidate = $"{baseName} {number}";
                if (!existingNames.Contains(candidate)) return candidate;
            }
        }

        private static string Slug(string name)
        {
            var builder = new StringBuilder();
            var pendingDash = false;
            foreach (var character in name ?? string.Empty)
            {
                if (character < 128 && char.IsLetterOrDigit(character))
                {
                    if (pendingDash && builder.Length > 0) builder.Append('-');
                    builder.Append(char.ToLowerInvariant(character));
                    pendingDash = false;
                }
                else
                {
                    pendingDash = true;
                }
            }

            return builder.Length > 0 ? builder.ToString() : FallbackId;
        }
    }
}
