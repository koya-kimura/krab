using System.Collections.Generic;
using Krab.TypeLayout.Core.Text;

namespace Krab.TypeLayout.Tests.Editor
{
    /// <summary>Hand-made font: "M" is wide, "i" narrow with off-center ink, space has no ink.</summary>
    internal sealed class FakeMetrics : IGlyphMetrics
    {
        private readonly Dictionary<string, GlyphMeasure> _measures = new()
        {
            ["M"] = new GlyphMeasure(0.8, 0.1, -0.7, 0.7, 0),
            ["e"] = new GlyphMeasure(0.5, 0.05, -0.5, 0.45, 0.01),
            ["i"] = new GlyphMeasure(0.25, 0.08, -0.72, 0.16, 0),
            ["g"] = new GlyphMeasure(0.5, 0.05, -0.5, 0.45, 0.2),
            [" "] = GlyphMeasure.Blank(0.3)
        };

        public double Ascent => 0.8;

        public double Descent => 0.2;

        public double CapHeight => 0.7;

        public GlyphMeasure Measure(string cluster) => _measures[cluster];

        public double[] Advances(string text, double size)
        {
            var clusters = TextClusters.Split(text);
            var advances = new double[clusters.Length];
            for (var i = 0; i < clusters.Length; i++)
                advances[i] = Measure(clusters[i]).Advance * size;

            return advances;
        }
    }
}
