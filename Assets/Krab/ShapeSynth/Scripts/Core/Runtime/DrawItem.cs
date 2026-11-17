using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;

namespace Krab.ShapeSynth.Core.Runtime
{
    /// <summary>Evaluated shape occurrence. Geometry stays local; WorldTransform is applied exactly once.</summary>
    public sealed class DrawItem
    {
        public ShapeKind Kind { get; }
        public RgbaColor FillColor { get; }
        public RgbaColor StrokeColor { get; }
        public ShapeStyle Style { get; }
        public IReadOnlyList<Point2> Vertices { get; }
        public Affine2D WorldTransform { get; }
        public double Width { get; }
        public double Height { get; }
        public double Opacity { get; }

        internal DrawItem(ShapeDefinition shape, Affine2D worldTransform, double width, double height, double opacity,
            RgbaColor fillColor, RgbaColor strokeColor)
        {
            Kind = shape.Kind;
            FillColor = fillColor;
            StrokeColor = strokeColor;
            Style = shape.Style;
            Vertices = shape.Vertices;
            WorldTransform = worldTransform;
            Width = width;
            Height = height;
            Opacity = opacity;
        }
    }
}
