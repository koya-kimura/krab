using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Evaluated shape occurrence. Geometry stays local; WorldTransform is applied exactly once.</summary>
    public sealed class DrawItem
    {
        public ShapeKind Kind { get; }
        public RgbaColor Color { get; }
        public IReadOnlyList<Point2> Vertices { get; }
        public Affine2D WorldTransform { get; }
        public double Width { get; }
        public double Height { get; }
        public double Opacity { get; }

        internal DrawItem(ShapeDefinition shape, Affine2D worldTransform, double width, double height, double opacity)
        {
            Kind = shape.Kind;
            Color = shape.Color;
            Vertices = shape.Vertices;
            WorldTransform = worldTransform;
            Width = width;
            Height = height;
            Opacity = opacity;
        }
    }
}
