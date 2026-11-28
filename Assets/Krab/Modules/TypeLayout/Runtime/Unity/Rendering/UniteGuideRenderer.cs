using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Unite.Canvas;
using UnityEngine;

namespace Krab.TypeLayout.Unity.Rendering
{
    /// <summary>Thin lines and dots for checking alignment by eye. Not meant for the final picture.</summary>
    public static class UniteGuideRenderer
    {
        public static void DrawRect(UCanvas canvas, Rect2 rect, Color color, float weight = 1)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));

            canvas.Push();
            try
            {
                canvas.NoFill();
                canvas.Stroke(color);
                canvas.StrokeWeight(weight);
                canvas.Rect((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
            }
            finally
            {
                canvas.Pop();
            }
        }

        /// <summary>Approximates the path with straight segments.</summary>
        public static void DrawPath(UCanvas canvas, ILayoutPath path, Color color, float weight = 1, int segments = 128)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (segments <= 0) throw new ArgumentOutOfRangeException(nameof(segments), segments, null);

            canvas.Push();
            try
            {
                canvas.Stroke(color);
                canvas.StrokeWeight(weight);
                var previous = path.Sample(0).Position;
                for (var i = 1; i <= segments; i++)
                {
                    var distance = path.Length * i / segments;
                    var next = path.IsClosed && i == segments ? path.Sample(0).Position : path.Sample(distance).Position;
                    canvas.Line((float)previous.X, (float)previous.Y, (float)next.X, (float)next.Y);
                    previous = next;
                }
            }
            finally
            {
                canvas.Pop();
            }
        }

        public static void DrawAnchors(UCanvas canvas, IReadOnlyList<GlyphPlacement> placements, Color color,
            float diameter = 6)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));
            if (placements == null) throw new ArgumentNullException(nameof(placements));

            canvas.Push();
            try
            {
                canvas.NoStroke();
                canvas.Fill(color);
                for (var i = 0; i < placements.Count; i++)
                    canvas.Circle((float)placements[i].Anchor.X, (float)placements[i].Anchor.Y, diameter);
            }
            finally
            {
                canvas.Pop();
            }
        }
    }
}
