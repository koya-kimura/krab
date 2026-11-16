using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Geometry;

namespace Krab.ShapeSynth.Core.Definitions
{
    public enum ShapeKind
    {
        Circle,
        Rectangle,
        Polygon
    }

    public sealed class ShapeDefinition : INodeDefinition
    {
        public ShapeKind Kind { get; }
        public TransformDefinition Transform { get; }
        public ParameterDefinition Width { get; }
        public ParameterDefinition Height { get; }
        public ParameterDefinition Opacity { get; }
        /// <summary>The base fill color (FillColor.From).</summary>
        public RgbaColor Color => FillColor.From;

        public ColorDefinition FillColor { get; }
        public ShapeStyle Style { get; }
        public IReadOnlyList<Point2> Vertices { get; }

        private ShapeDefinition(ShapeKind kind, ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform, ParameterDefinition opacity, RgbaColor? color, Point2[] vertices,
            ShapeStyle style)
        {
            Kind = kind;
            Width = width ?? throw new ArgumentNullException(nameof(width));
            Height = height ?? throw new ArgumentNullException(nameof(height));
            Transform = transform ?? new TransformDefinition();
            Opacity = opacity ?? ParameterDefinition.Constant(1);
            FillColor = ColorDefinition.Constant(color ?? RgbaColor.White);
            Style = style ?? new ShapeStyle();
            Vertices = Array.AsReadOnly(vertices);
        }

        private ShapeDefinition(ShapeDefinition source, ColorDefinition fillColor)
        {
            Kind = source.Kind;
            Width = source.Width;
            Height = source.Height;
            Transform = source.Transform;
            Opacity = source.Opacity;
            FillColor = fillColor ?? throw new ArgumentNullException(nameof(fillColor));
            Style = source.Style;
            Vertices = source.Vertices;
        }

        /// <summary>Returns a copy whose fill color is evaluated from fillColor; this Definition is unchanged.</summary>
        public ShapeDefinition WithFillColor(ColorDefinition fillColor) => new ShapeDefinition(this, fillColor);

        public static ShapeDefinition Circle(ParameterDefinition diameter, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null, ShapeStyle style = null) =>
            new ShapeDefinition(ShapeKind.Circle, diameter, diameter, transform, opacity, color,
                Array.Empty<Point2>(), style);

        public static ShapeDefinition Rectangle(ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform = null, ParameterDefinition opacity = null, RgbaColor? color = null,
            ShapeStyle style = null) =>
            new ShapeDefinition(ShapeKind.Rectangle, width, height, transform, opacity, color,
                Array.Empty<Point2>(), style);

        /// <summary>Vertices are caller-defined offsets from local (0,0); never recentered by their bounds.</summary>
        public static ShapeDefinition Polygon(IReadOnlyList<Point2> vertices, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null, ShapeStyle style = null)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            if (vertices.Count < 3)
                throw new ArgumentException("A polygon needs at least three vertices.", nameof(vertices));

            var copy = new Point2[vertices.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = vertices[i];
            }

            return new ShapeDefinition(ShapeKind.Polygon, ParameterDefinition.Constant(0),
                ParameterDefinition.Constant(0), transform, opacity, color, copy, style);
        }
    }
}
