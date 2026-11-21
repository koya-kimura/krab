namespace Krab.ImageLayer.Performance.Background
{
    public enum BackgroundStyle
    {
        /// <summary>Nothing drawn; the output camera's clear color shows.</summary>
        None,

        /// <summary>The palette's background color.</summary>
        Solid,

        /// <summary>Straight grid lines.</summary>
        Grid,

        /// <summary>Grid lines bent by moving waves.</summary>
        WarpGrid,

        /// <summary>Horizontal bands of the background and accent colors.</summary>
        Stripes,

        /// <summary>Checkerboard of the background and accent colors.</summary>
        Checker,
    }
}
