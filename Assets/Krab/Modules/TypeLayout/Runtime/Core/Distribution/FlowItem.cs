using Krab.TypeLayout.Core.Paths;

namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>One glyph of flowed text; the slot is the center of its advance box on the path.</summary>
    public readonly struct FlowItem
    {
        public FlowItem(int clusterIndex, double distance, LayoutSlot slot)
        {
            ClusterIndex = clusterIndex;
            Distance = distance;
            Slot = slot;
        }

        /// <summary>Index into the source clusters (repeats wrap back to 0).</summary>
        public int ClusterIndex { get; }

        /// <summary>Distance of the box center from the path start, before wrapping.</summary>
        public double Distance { get; }

        public LayoutSlot Slot { get; }
    }
}
