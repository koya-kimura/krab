namespace Krab.ImageLayer.Core.Placement
{
    /// <summary>How an image is scaled into a box, keeping its aspect ratio.</summary>
    public enum FitMode
    {
        /// <summary>Whole reference area visible inside the box (letterboxed).</summary>
        Contain,

        /// <summary>Box completely covered; the reference area may overflow it.</summary>
        Cover,
    }
}
