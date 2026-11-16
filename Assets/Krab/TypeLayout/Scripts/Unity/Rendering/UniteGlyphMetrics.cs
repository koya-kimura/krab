using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Text;
using Unite.Canvas;
using Unite.Text;
using Unite.Text.Fonts;
using UnityEngine;

namespace Krab.TypeLayout.Unity.Rendering
{
    /// <summary>Measures clusters with p5.Unite Text, once per cluster, and reports them at size 1.</summary>
    public sealed class UniteGlyphMetrics : IGlyphMetrics
    {
        // Large enough that float rounding inside the text layout stays far below a pixel at any drawn size.
        private const float MeasureSize = 1000f;

        private readonly UCanvas _canvas;
        private readonly FontFace _font;
        private readonly Dictionary<string, GlyphMeasure> _measures = new();

        public UniteGlyphMetrics(UCanvas canvas, FontFace font)
        {
            _canvas = canvas ? canvas : throw new ArgumentNullException(nameof(canvas));
            _font = font ?? throw new ArgumentNullException(nameof(font));
            Ascent = (double)font.Ascender / font.UnitsPerEm;
            Descent = (double)-font.Descender / font.UnitsPerEm;

            // The font tables Unite exposes have no cap height, so measure a flat capital.
            var capital = Measure("H");
            CapHeight = capital.HasInk ? -capital.InkTop : Ascent * 0.7;
        }

        public double Ascent { get; }

        public double Descent { get; }

        public double CapHeight { get; }

        public GlyphMeasure Measure(string cluster)
        {
            if (cluster == null) throw new ArgumentNullException(nameof(cluster));
            if (_measures.TryGetValue(cluster, out var cached)) return cached;

            _canvas.Push();
            try
            {
                _canvas.TextFont(_font, MeasureSize);
                _canvas.TextAlign(TextAlignMode.Left, TextVerticalAlignMode.Baseline);
                var advance = Advance(cluster);
                var ink = _canvas.TextBounds(cluster, 0, 0);
                var measure = ink.width > 0 && ink.height > 0
                    ? new GlyphMeasure(advance, ink.xMin / MeasureSize, ink.yMin / MeasureSize,
                        ink.xMax / MeasureSize, ink.yMax / MeasureSize)
                    : GlyphMeasure.Blank(advance);
                _measures.Add(cluster, measure);
                return measure;
            }
            finally
            {
                _canvas.Pop();
            }
        }

        // Single characters read the font's advance directly: the shaped line width drops trailing spaces.
        private double Advance(string cluster)
        {
            var isSingle = cluster.Length == 1 && !char.IsSurrogate(cluster[0])
                           || cluster.Length == 2 && char.IsSurrogatePair(cluster, 0);
            var glyphId = isSingle ? _font.GetGlyphId(char.ConvertToUtf32(cluster, 0)) : 0;
            if (glyphId != 0) return (double)_font.Default.Advance(glyphId) / _font.UnitsPerEm;

            return _canvas.FontWidth(cluster) / MeasureSize;
        }
    }
}
