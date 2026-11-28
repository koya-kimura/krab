namespace Krab.TypeLayout.Core.Text
{
    /// <summary>Font metrics at size 1. Implemented by the rendering adapter; tests use fixed values.</summary>
    public interface IGlyphMetrics
    {
        /// <summary>Distance from the baseline up to the top of the em box (positive).</summary>
        public double Ascent { get; }

        /// <summary>Distance from the baseline down to the bottom of the em box (positive).</summary>
        public double Descent { get; }

        /// <summary>Height of flat capital letters above the baseline (positive).</summary>
        public double CapHeight { get; }

        public GlyphMeasure Measure(string cluster);
    }
}
