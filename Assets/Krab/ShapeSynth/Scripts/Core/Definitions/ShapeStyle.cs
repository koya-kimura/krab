using System;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Which outlines a Shape draws. Colors live on the Shape as fill and stroke ColorDefinitions.</summary>
    public sealed class ShapeStyle
    {
        public bool FillEnabled { get; }
        public bool StrokeEnabled { get; }
        public double StrokeWidth { get; }

        public ShapeStyle(bool fill = true, bool stroke = false, double strokeWidth = 1)
        {
            VoiceTime.RequireFinite(strokeWidth, nameof(strokeWidth));
            if (strokeWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(strokeWidth), "Stroke width cannot be negative.");

            FillEnabled = fill;
            StrokeEnabled = stroke;
            StrokeWidth = strokeWidth;
        }
    }
}
