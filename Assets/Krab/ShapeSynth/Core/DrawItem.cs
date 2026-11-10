using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Evaluated shape occurrence. Geometry stays local; WorldTransform is applied exactly once.</summary>
    public sealed class DrawItem
    {
        public ShapeDefinition Shape { get; }
        public ShapeKind Kind => Shape.Kind;
        public RgbaColor Color => Shape.Color;
        public IReadOnlyList<Point2> Vertices => Shape.Vertices;
        public Affine2D WorldTransform { get; }
        public double Width { get; }
        public double Height { get; }
        public double Opacity { get; }

        internal DrawItem(ShapeDefinition shape, Affine2D worldTransform, double width, double height, double opacity)
        {
            Shape = shape;
            WorldTransform = worldTransform;
            Width = width;
            Height = height;
            Opacity = opacity;
        }
    }
}
