using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core;
using Unite.Canvas;
using UnityEngine;
using CoreShapeKind = Krab.ShapeSynth.Core.ShapeKind;

namespace Krab.ShapeSynth.Unity
{
    /// <summary>Minimal DrawItem boundary. No hierarchy traversal, parameter evaluation or TRS decomposition.</summary>
    public static class UniteShapeAdapter
    {
        public static void Draw(UCanvas canvas, IReadOnlyList<DrawItem> items, float width, float height,
            bool invertTargetY = false)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var item in items)
            {
                if (item.Kind != CoreShapeKind.Polygon && (item.Width <= 0 || item.Height <= 0)) continue;
                var matrix = item.WorldTransform;
                var color = item.Color;
                var alpha = color.A * item.Opacity;
                if (double.IsNaN(alpha) || double.IsInfinity(alpha)) throw new ArgumentOutOfRangeException(nameof(items));
                canvas.Push();
                try
                {
                    canvas.ResetMatrix();
                    canvas.Translate(width * 0.5f, height * 0.5f);
                    if (invertTargetY) canvas.Scale(1, -1);
                    // p5's six arguments are column-major a,b,c,d,e,f.
                    canvas.ApplyMatrix(Float(matrix.M00), Float(matrix.M10), Float(matrix.M01),
                        Float(matrix.M11), Float(matrix.M02), Float(matrix.M12));
                    canvas.NoStroke();
                    canvas.Fill(new Color(Float(color.R), Float(color.G), Float(color.B), (float)Math.Max(0, Math.Min(1, alpha))));
                    canvas.RectMode = ShapeMode.Center;
                    canvas.EllipseMode = ShapeMode.Center;
                    switch (item.Kind)
                    {
                        case CoreShapeKind.Circle:
                            canvas.Circle(0, 0, Float(item.Width));
                            break;
                        case CoreShapeKind.Rectangle:
                            canvas.Rect(0, 0, Float(item.Width), Float(item.Height));
                            break;
                        case CoreShapeKind.Polygon:
                            canvas.BeginShape();
                            foreach (var point in item.Vertices) canvas.Vertex(Float(point.X), Float(point.Y));
                            canvas.EndShape(ShapeCloseMode.Close);
                            break;
                        default: throw new ArgumentOutOfRangeException(nameof(items));
                    }
                }
                finally { canvas.Pop(); }
            }
        }

        private static float Float(double value)
        {
            var result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result))
                throw new ArgumentOutOfRangeException(nameof(value), "Draw value is outside finite float range.");
            return result;
        }
    }
}
