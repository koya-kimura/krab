namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// Which outlines a Shape draws. Colors and the stroke width live on the Shape as Parameters, so they can be
    /// bound like any other value.
    /// </summary>
    public sealed class ShapeStyle
    {
        public bool FillEnabled { get; }
        public bool StrokeEnabled { get; }

        public ShapeStyle(bool fill = true, bool stroke = false)
        {
            FillEnabled = fill;
            StrokeEnabled = stroke;
        }
    }
}
