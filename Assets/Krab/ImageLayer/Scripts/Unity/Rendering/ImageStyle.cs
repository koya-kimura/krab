using UnityEngine;

namespace Krab.ImageLayer.Unity.Rendering
{
    /// <summary>Color treatment applied to each image() call; part of the Push/Pop state.</summary>
    public struct ImageStyle
    {
        public static ImageStyle Default => new()
        {
            Tint = Color.white,
            Dark = Color.black,
            Light = Color.white,
            Duotone = 0,
            ThresholdEnabled = false,
            ThresholdLevel = 0.5f,
            ThresholdSoftness = 0.02f
        };

        /// <summary>Multiplies the final color; alpha fades the image.</summary>
        public Color Tint;

        /// <summary>Colors that dark and light parts of the image map to.</summary>
        public Color Dark;

        public Color Light;

        /// <summary>0 = original colors, 1 = fully mapped to Dark..Light.</summary>
        public float Duotone;

        /// <summary>Splits lightness into the two colors at a level, for a high-contrast cut-out look.</summary>
        public bool ThresholdEnabled;

        public float ThresholdLevel;

        public float ThresholdSoftness;
    }
}
