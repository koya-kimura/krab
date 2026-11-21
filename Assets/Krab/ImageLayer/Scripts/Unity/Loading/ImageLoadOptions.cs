using System;

namespace Krab.ImageLayer.Unity.Loading
{
    /// <summary>How frames are turned into textures.</summary>
    [Serializable]
    public struct ImageLoadOptions
    {
        public static ImageLoadOptions Default => new()
        {
            Compress = true,
            Mipmaps = true,
            AlphaThreshold = 0,
            FrameBudgetMilliseconds = 12
        };

        /// <summary>Block-compress to DXT5/BC3: about a quarter of the memory, with some loss on fine detail.</summary>
        public bool Compress;

        /// <summary>Smaller copies for drawing at reduced size; avoids shimmer when many small images move.</summary>
        public bool Mipmaps;

        /// <summary>Pixels with alpha at or below this count as transparent margin.</summary>
        public byte AlphaThreshold;

        /// <summary>Decoding time spent per Unity frame before yielding, so the output keeps running.</summary>
        public float FrameBudgetMilliseconds;
    }
}
