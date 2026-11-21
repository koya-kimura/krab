namespace Krab.ImageLayer.Performance
{
    /// <summary>Color treatment of a pattern's images, using the shared palette.</summary>
    public enum ImageLook
    {
        /// <summary>The photo's own colors.</summary>
        Original,

        /// <summary>Lightness mapped from the palette's dark to light color.</summary>
        Duotone,

        /// <summary>Hard cut into the two palette colors at the shared threshold.</summary>
        Threshold,
    }
}
