using System;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable fill and stroke settings for a shape.</summary>
    public sealed class ShapeStyle
    {
        public bool FillEnabled { get; }
        public bool StrokeEnabled { get; }
        public double StrokeWidth { get; }
        public RgbaColor? StrokeColor { get; }

        public ShapeStyle(bool fill = true, bool stroke = false, double strokeWidth = 1,
            RgbaColor? strokeColor = null)
        {
            VoiceTime.RequireFinite(strokeWidth, nameof(strokeWidth));
            if (strokeWidth < 0) throw new ArgumentOutOfRangeException(nameof(strokeWidth), "Stroke width cannot be negative.");
            FillEnabled = fill;
            StrokeEnabled = stroke;
            StrokeWidth = strokeWidth;
            StrokeColor = strokeColor;
        }
    }
}
