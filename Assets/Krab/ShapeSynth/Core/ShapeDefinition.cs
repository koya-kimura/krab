using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    public enum ShapeKind { Circle, Rectangle, Polygon }

    public sealed class ShapeDefinition : INodeDefinition
    {
        public ShapeKind Kind { get; }
        public TransformDefinition Transform { get; }
        public ParameterDefinition Width { get; }
        public ParameterDefinition Height { get; }
        public ParameterDefinition Opacity { get; }
        public RgbaColor Color { get; }
        public IReadOnlyList<Point2> Vertices { get; }

        private ShapeDefinition(ShapeKind kind, ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform, ParameterDefinition opacity, RgbaColor? color, Point2[] vertices)
        {
            Kind = kind;
            Width = width ?? throw new ArgumentNullException(nameof(width));
            Height = height ?? throw new ArgumentNullException(nameof(height));
            Transform = transform ?? new TransformDefinition();
            Opacity = opacity ?? ParameterDefinition.Constant(1);
            Color = color ?? RgbaColor.White;
            Vertices = Array.AsReadOnly(vertices);
        }

        public static ShapeDefinition Circle(ParameterDefinition diameter, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null) =>
            new ShapeDefinition(ShapeKind.Circle, diameter, diameter, transform, opacity, color, Array.Empty<Point2>());

        public static ShapeDefinition Rectangle(ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform = null, ParameterDefinition opacity = null, RgbaColor? color = null) =>
            new ShapeDefinition(ShapeKind.Rectangle, width, height, transform, opacity, color, Array.Empty<Point2>());

        /// <summary>Vertices are caller-defined offsets from local (0,0); never recentered by their bounds.</summary>
        public static ShapeDefinition Polygon(IReadOnlyList<Point2> vertices, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (vertices.Count < 3) throw new ArgumentException("A polygon needs at least three vertices.", nameof(vertices));
            var copy = new Point2[vertices.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = vertices[i];
            return new ShapeDefinition(ShapeKind.Polygon, ParameterDefinition.Constant(0), ParameterDefinition.Constant(0),
                transform, opacity, color, copy);
        }
    }
}
