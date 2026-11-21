using System;
using System.Collections.Generic;
using Krab.TypeLayout.Unity.Rendering;

namespace Krab.TypeLayout.Performance.Patterns
{
    /// <summary>
    /// A block of lines (split by |) laid out left to right once per text and size: each cluster's pen x, line and
    /// word. Rebuilt only when the text or size changes.
    /// </summary>
    internal sealed class FlickerText
    {
        private readonly List<Glyph> _glyphs = new();

        private string _source;
        private double _size;

        public IReadOnlyList<Glyph> Glyphs => _glyphs;

        public int LineCount { get; private set; }

        /// <summary>Width of the longest line in output pixels.</summary>
        public double Width { get; private set; }

        public void Update(string text, double size, TypeLayoutDrawer drawer)
        {
            if (text == _source && size == _size) return;

            _source = text;
            _size = size;
            _glyphs.Clear();
            LineCount = 0;
            Width = 0;

            var wordCount = 0;
            foreach (var line in (text ?? string.Empty).Split('|'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;

                var x = 0.0;
                var inWord = false;
                foreach (var cluster in drawer.Clusters(trimmed))
                {
                    var advance = drawer.Metrics.Measure(cluster).Advance * size;
                    var blank = string.IsNullOrWhiteSpace(cluster);
                    if (!blank && !inWord) wordCount++;

                    inWord = !blank;
                    _glyphs.Add(new Glyph(cluster, LineCount, x, advance, blank ? -1 : wordCount - 1));
                    x += advance;
                }

                Width = Math.Max(Width, x);
                LineCount++;
            }
        }

        public readonly struct Glyph
        {
            public Glyph(string text, int line, double x, double advance, int word)
            {
                Text = text;
                Line = line;
                X = x;
                Advance = advance;
                Word = word;
            }

            public string Text { get; }

            public int Line { get; }

            /// <summary>Pen x from the start of the line, in output pixels.</summary>
            public double X { get; }

            public double Advance { get; }

            /// <summary>Index of the word across the block, or -1 for spaces.</summary>
            public int Word { get; }

            public bool IsBlank => Word < 0;
        }
    }
}
