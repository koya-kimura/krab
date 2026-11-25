using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;

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
        public ColorDefinition FillColor { get; }

        /// <summary>The same instance as FillColor unless a separate stroke color was given.</summary>
        public ColorDefinition StrokeColor { get; }

        /// <summary>Local stroke width; a value of 0 or below draws no stroke.</summary>
        public ParameterDefinition StrokeWidth { get; }

        public ShapeStyle Style { get; }
        public IReadOnlyList<Point2> Vertices { get; }

        /// <summary>
        /// The point of the Shape's bounds that sits at its local origin, so Position places it and Rotation and
        /// Scale turn around it. −0.5 to 0.5 per axis: (0, 0) is the center, (0, −0.5) the bottom edge, (0.5, 0.5)
        /// the top-right corner. Bounds are Width × Height for Circle and Rectangle, the vertex box for Polygon.
        /// </summary>
        public Point2 Anchor { get; private set; }

        private ShapeDefinition(ShapeKind kind, ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform, ParameterDefinition opacity, RgbaColor? color, Point2[] vertices,
            ShapeStyle style, ColorDefinition fillColor, ColorDefinition strokeColor, ParameterDefinition strokeWidth)
        {
            Kind = kind;
            Width = width ?? throw new ArgumentNullException(nameof(width));
            Height = height ?? throw new ArgumentNullException(nameof(height));
            Transform = transform ?? new TransformDefinition();
            Opacity = opacity ?? ParameterDefinition.Constant(1);
            FillColor = fillColor ?? (color.HasValue ? ColorDefinition.Constant(color.Value) : ColorDefinition.White);
            StrokeColor = strokeColor ?? FillColor;
            StrokeWidth = strokeWidth ?? ParameterDefinition.Constant(1);
            Style = style ?? new ShapeStyle();
            Vertices = Array.AsReadOnly(vertices);
        }

        // fillColor, when given, replaces the constant color; strokeColor defaults to the fill color and
        // strokeWidth to a constant 1.
        public static ShapeDefinition Circle(ParameterDefinition diameter, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null, ShapeStyle style = null,
            ColorDefinition fillColor = null, ColorDefinition strokeColor = null,
            ParameterDefinition strokeWidth = null) =>
            new ShapeDefinition(ShapeKind.Circle, diameter, diameter, transform, opacity, color,
                Array.Empty<Point2>(), style, fillColor, strokeColor, strokeWidth);

        public static ShapeDefinition Rectangle(ParameterDefinition width, ParameterDefinition height,
            TransformDefinition transform = null, ParameterDefinition opacity = null, RgbaColor? color = null,
            ShapeStyle style = null, ColorDefinition fillColor = null, ColorDefinition strokeColor = null,
            ParameterDefinition strokeWidth = null) =>
            new ShapeDefinition(ShapeKind.Rectangle, width, height, transform, opacity, color,
                Array.Empty<Point2>(), style, fillColor, strokeColor, strokeWidth);

        /// <summary>Vertices are caller-defined offsets from local (0,0); never recentered by their bounds.</summary>
        public static ShapeDefinition Polygon(IReadOnlyList<Point2> vertices, TransformDefinition transform = null,
            ParameterDefinition opacity = null, RgbaColor? color = null, ShapeStyle style = null,
            ColorDefinition fillColor = null, ColorDefinition strokeColor = null,
            ParameterDefinition strokeWidth = null)
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
                ParameterDefinition.Constant(0), transform, opacity, color, copy, style, fillColor, strokeColor,
                strokeWidth);
        }

        /// <summary>The same Shape with another anchor; each coordinate is finite, from −0.5 to 0.5.</summary>
        public ShapeDefinition WithAnchor(double x, double y)
        {
            VoiceTime.RequireFinite(x, nameof(x));
            VoiceTime.RequireFinite(y, nameof(y));
            if (Math.Abs(x) > 0.5 || Math.Abs(y) > 0.5)
                throw new ArgumentOutOfRangeException(nameof(x), "Anchor coordinates must be from -0.5 to 0.5.");

            var copy = (ShapeDefinition)MemberwiseClone();
            copy.Anchor = new Point2(x, y);
            return copy;
        }

        /// <summary>The local offset that puts the anchor on the origin, for the evaluated Width and Height.</summary>
        public Point2 AnchorOffset(double width, double height)
        {
            if (Anchor.X == 0 && Anchor.Y == 0) return new Point2(0, 0);
            if (Kind != ShapeKind.Polygon) return new Point2(-Anchor.X * width, -Anchor.Y * height);

            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var vertex in Vertices)
            {
                minX = Math.Min(minX, vertex.X);
                maxX = Math.Max(maxX, vertex.X);
                minY = Math.Min(minY, vertex.Y);
                maxY = Math.Max(maxY, vertex.Y);
            }

            return new Point2(-(minX + (Anchor.X + 0.5) * (maxX - minX)), -(minY + (Anchor.Y + 0.5) * (maxY - minY)));
        }
    }
}
