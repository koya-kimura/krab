namespace Krab.ImageLayer.Core.Placement
{
    /// <summary>How the four numbers of an image() call are read, as in p5.js imageMode().</summary>
    public enum ImageMode
    {
        /// <summary>x, y = top-left corner; then width, height.</summary>
        Corner,

        /// <summary>x, y = center; then width, height.</summary>
        Center,

        /// <summary>x1, y1, x2, y2 = two opposite corners, in any order.</summary>
        Corners,
    }
}
