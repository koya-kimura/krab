using Krab.ImageLayer.Unity.Library;
using Krab.ImageLayer.Unity.Rendering;
using UnityEngine;

namespace Krab.ImageLayer.Performance
{
    /// <summary>What every image pattern needs for one frame. Lengths are output pixels unless noted.</summary>
    public readonly struct ImagePatternContext
    {
        private const double ReferenceHeight = 1080;

        public ImagePatternContext(ImageCanvas canvas, ImageLibrary library, float width, float height, double beats,
            float bpm, Color dark, Color light, float thresholdLevel)
        {
            Canvas = canvas;
            Library = library;
            Width = width;
            Height = height;
            Beats = beats;
            Bpm = bpm;
            Dark = dark;
            Light = light;
            ThresholdLevel = thresholdLevel;
        }

        public ImageCanvas Canvas { get; }

        public ImageLibrary Library { get; }

        public float Width { get; }

        public float Height { get; }

        /// <summary>Running beat count of the tempo clock.</summary>
        public double Beats { get; }

        public float Bpm { get; }

        /// <summary>Dark and light ends of the Duotone and Threshold looks.</summary>
        public Color Dark { get; }

        public Color Light { get; }

        public float ThresholdLevel { get; }

        /// <summary>Reference pixels (1080 tall) to output pixels.</summary>
        public float Px(double reference) => (float)(reference * Height / ReferenceHeight);
    }
}
