using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Unity.Rendering;
using UnityEngine;

namespace Krab.TypeLayout.Performance
{
    /// <summary>What every pattern needs for one frame.</summary>
    public readonly struct PatternContext
    {
        public PatternContext(LayoutFrame frame, Rect2 safeArea, double beats, float bpm, TypeLayoutDrawer drawer,
            Color color)
        {
            Frame = frame;
            SafeArea = safeArea;
            Beats = beats;
            Bpm = bpm;
            Drawer = drawer;
            Color = color;
        }

        public LayoutFrame Frame { get; }

        /// <summary>Output bounds inset by the shared margin, in output pixels.</summary>
        public Rect2 SafeArea { get; }

        /// <summary>Running beat count of the tempo clock.</summary>
        public double Beats { get; }

        public float Bpm { get; }

        public TypeLayoutDrawer Drawer { get; }

        public Color Color { get; }

        /// <summary>Reference pixels (1080 tall) to output pixels.</summary>
        public double Px(double reference) => Frame.ToPixels(reference);
    }
}
