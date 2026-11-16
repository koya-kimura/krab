using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Unity.Rendering;
using UnityEngine;

namespace Krab.TypeLayout.Performance
{
    /// <summary>What every layer needs for one frame.</summary>
    public readonly struct LayerContext
    {
        public LayerContext(LayoutFrame frame, Rect2 safeArea, double beats, TypeLayoutDrawer drawer, Color color)
        {
            Frame = frame;
            SafeArea = safeArea;
            Beats = beats;
            Drawer = drawer;
            Color = color;
        }

        public LayoutFrame Frame { get; }

        /// <summary>Output bounds inset by the shared margin, in output pixels.</summary>
        public Rect2 SafeArea { get; }

        /// <summary>Running beat count of the tempo clock.</summary>
        public double Beats { get; }

        public TypeLayoutDrawer Drawer { get; }

        public Color Color { get; }
    }
}
