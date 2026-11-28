using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Placement;
using Unite.Canvas;
using Unite.Text;
using Unite.Text.Fonts;
using UnityEngine;

namespace Krab.TypeLayout.Unity.Rendering
{
    public static class UniteTextRenderer
    {
        /// <summary>Draws each placement left- and baseline-aligned at its pen origin, in list order.</summary>
        public static void Draw(UCanvas canvas, FontFace font, IReadOnlyList<GlyphPlacement> placements, Color color)
        {
            if (!canvas) throw new ArgumentNullException(nameof(canvas));
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (placements == null) throw new ArgumentNullException(nameof(placements));

            canvas.Push();
            try
            {
                canvas.TextFont(font);
                canvas.TextAlign(TextAlignMode.Left, TextVerticalAlignMode.Baseline);
                canvas.NoStroke();
                canvas.Fill(color);
                for (var i = 0; i < placements.Count; i++)
                    DrawGlyph(canvas, placements[i]);
            }
            finally
            {
                canvas.Pop();
            }
        }

        private static void DrawGlyph(UCanvas canvas, GlyphPlacement placement)
        {
            canvas.Push();
            try
            {
                canvas.Translate((float)placement.Origin.X, (float)placement.Origin.Y);
                canvas.Rotate((float)Angles.ToRadians(placement.RotationDegrees));
                canvas.TextSize((float)placement.Size);
                canvas.Text(placement.Text, 0, 0);
            }
            finally
            {
                canvas.Pop();
            }
        }
    }
}
