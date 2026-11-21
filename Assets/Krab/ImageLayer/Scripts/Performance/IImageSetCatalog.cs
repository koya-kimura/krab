using System.Collections.Generic;

namespace Krab.ImageLayer.Performance
{
    /// <summary>Set names of the loaded library, for pattern dropdowns that rebuild after a reload.</summary>
    public interface IImageSetCatalog
    {
        public IReadOnlyList<string> SetNames { get; }

        /// <summary>Changes whenever a different library finishes loading.</summary>
        public int Version { get; }
    }
}
