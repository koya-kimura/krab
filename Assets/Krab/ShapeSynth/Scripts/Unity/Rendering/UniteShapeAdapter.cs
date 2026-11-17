using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Runtime;
using Unite.Canvas;
using UnityEngine;
using CoreShapeKind = Krab.ShapeSynth.Core.Definitions.ShapeKind;

namespace Krab.ShapeSynth.Unity.Rendering
{
    /// <summary>Minimal DrawItem boundary. No hierarchy traversal, parameter evaluation or TRS decomposition.</summary>
    public static class UniteShapeAdapter
    {
        public static Matrix4x4 CreateTargetMatrix(Affine2D transform, float width, float height,
            bool invertTargetY = false)
        {
            ValidateViewport(width, height);
            var m00 = Float(transform.M00, nameof(transform));
            var m01 = Float(transform.M01, nameof(transform));
            var m02 = Float(transform.M02, nameof(transform));
            var m10 = Float(transform.M10, nameof(transform));
            var m11 = Float(transform.M11, nameof(transform));
            var m12 = Float(transform.M12, nameof(transform));
            var ySign = invertTargetY ? -1f : 1f;
            var matrix = Matrix4x4.identity;
            matrix.m00 = m00;
            matrix.m01 = m01;
            matrix.m03 = width * 0.5f + m02;
            matrix.m10 = ySign * m10;
            matrix.m11 = ySign * m11;
            matrix.m13 = height * 0.5f + ySign * m12;
            if (!Finite(matrix.m03) || !Finite(matrix.m13))
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Viewport translation must be finite.");
            }

            return matrix;
        }

        public static void Draw(UCanvas canvas, IReadOnlyList<DrawItem> items, float width, float height,
            bool invertTargetY = false)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));
            if (items == null) throw new ArgumentNullException(nameof(items));
            ValidateViewport(width, height);

            for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                var item = items[itemIndex];
                if (item == null)
                {
                    throw new ArgumentException($"Draw item at index {itemIndex} is null.", nameof(items));
                }

                if (item.Kind != CoreShapeKind.Polygon && (item.Width <= 0 || item.Height <= 0)) continue;

                if (item.Kind != CoreShapeKind.Circle
                    && item.Kind != CoreShapeKind.Rectangle
                    && item.Kind != CoreShapeKind.Polygon)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(items), $"Unsupported shape kind at index {itemIndex}.");
                }

                var style = item.Style ?? new ShapeStyle();
                if (!style.FillEnabled && (!style.StrokeEnabled || style.StrokeWidth <= 0)) continue;

                // Convert every submitted value before mutating the canvas or opening a polygon path.
                var matrix = CreateTargetMatrix(item.WorldTransform, width, height, invertTargetY);
                var strokeWidth = ResolveStrokeWidth(item.Kind, style.StrokeWidth, item.WorldTransform);
                var fill = style.FillEnabled ? MakeColor(item.FillColor, item.Opacity, nameof(items)) : Color.white;
                var stroke = style.StrokeEnabled && strokeWidth > 0
                    ? MakeColor(item.StrokeColor, item.Opacity, nameof(items))
                    : Color.black;
                var itemWidth = Float(item.Width, nameof(items));
                var itemHeight = Float(item.Height, nameof(items));
                float[] polygon = null;
                if (item.Kind == CoreShapeKind.Polygon)
                {
                    if (item.Vertices == null)
                    {
                        throw new ArgumentException($"Polygon at index {itemIndex} has no vertices.", nameof(items));
                    }

                    if (item.Vertices.Count < 3)
                    {
                        throw new ArgumentException(
                            $"Polygon at index {itemIndex} needs at least three vertices.", nameof(items));
                    }

                    polygon = new float[item.Vertices.Count * 2];
                    for (var i = 0; i < item.Vertices.Count; i++)
                    {
                        polygon[i * 2] = Float(item.Vertices[i].X, nameof(items));
                        polygon[i * 2 + 1] = Float(item.Vertices[i].Y, nameof(items));
                    }
                }

                canvas.Push();
                try
                {
                    canvas.ResetMatrix();
                    canvas.ApplyMatrix(matrix.m00, matrix.m10, matrix.m01, matrix.m11, matrix.m03, matrix.m13);
                    if (style.FillEnabled)
                    {
                        canvas.Fill(fill);
                    }
                    else
                    {
                        canvas.NoFill();
                    }

                    if (style.StrokeEnabled && strokeWidth > 0)
                    {
                        canvas.Stroke(stroke);
                        canvas.StrokeWeight(strokeWidth);
                    }
                    else
                    {
                        canvas.NoStroke();
                    }

                    canvas.RectMode = ShapeMode.Center;
                    canvas.EllipseMode = ShapeMode.Center;
                    switch (item.Kind)
                    {
                        case CoreShapeKind.Circle:
                            canvas.Circle(0, 0, itemWidth);
                            break;
                        case CoreShapeKind.Rectangle:
                            canvas.Rect(0, 0, itemWidth, itemHeight);
                            break;
                        case CoreShapeKind.Polygon:
                            canvas.BeginShape();
                            for (var i = 0; i < polygon.Length; i += 2)
                            {
                                canvas.Vertex(polygon[i], polygon[i + 1]);
                            }

                            canvas.EndShape(ShapeCloseMode.Close);
                            break;
                    }
                }
                finally
                {
                    canvas.Pop();
                }
            }
        }

        /// <summary>
        /// Polygon paths expand lines after transforming endpoints; use area scale as a scalar approximation.
        /// </summary>
        public static float ResolveStrokeWidth(CoreShapeKind kind, double localWidth, Affine2D transform)
        {
            if (double.IsNaN(localWidth) || double.IsInfinity(localWidth) || localWidth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localWidth));
            }

            var scale = kind == CoreShapeKind.Polygon
                ? Math.Sqrt(Math.Abs(transform.M00 * transform.M11 - transform.M01 * transform.M10))
                : 1;
            return Float(localWidth * scale, nameof(localWidth));
        }

        private static void ValidateViewport(float width, float height)
        {
            if (!Finite(width) || width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Viewport width must be finite and positive.");
            }

            if (!Finite(height) || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Viewport height must be finite and positive.");
            }
        }

        private static float Float(double value, string parameterName)
        {
            var result = (float)value;
            if (!Finite(result))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Draw value is outside finite float range.");
            }

            return result;
        }

        private static Color MakeColor(RgbaColor color, double opacity, string parameterName)
        {
            var red = Float(color.R, parameterName);
            var green = Float(color.G, parameterName);
            var blue = Float(color.B, parameterName);
            var alphaValue = color.A * opacity;
            if (double.IsNaN(alphaValue) || double.IsInfinity(alphaValue))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Draw alpha must be finite.");
            }

            var alpha = (float)Math.Max(0, Math.Min(1, alphaValue));
            return new Color(red, green, blue, alpha);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
