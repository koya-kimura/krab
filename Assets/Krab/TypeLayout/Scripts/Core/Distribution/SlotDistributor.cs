using System;
using Krab.TypeLayout.Core.Paths;

namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>Places a fixed number of evenly spaced slots on a path, one glyph each.</summary>
    public static class SlotDistributor
    {
        public static LayoutSlot[] Distribute(ILayoutPath path, int count, SlotSpacing spacing)
        {
            Require.NotNull(path, nameof(path));
            Require.Positive(count, nameof(count));

            var slots = new LayoutSlot[count];
            for (var i = 0; i < count; i++)
                slots[i] = path.Sample(Distance(path, count, spacing, i));

            return slots;
        }

        public static double Distance(ILayoutPath path, int count, SlotSpacing spacing, int index)
        {
            Require.NotNull(path, nameof(path));
            Require.Positive(count, nameof(count));
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index), index, null);

            // Multiply before dividing so the last of an open Ends layout lands on the end exactly.
            return spacing switch
            {
                SlotSpacing.Ends when path.IsClosed => path.Length * index / count,
                SlotSpacing.Ends when count == 1 => path.Length / 2,
                SlotSpacing.Ends => path.Length * index / (count - 1),
                SlotSpacing.Cells => path.Length * (2 * index + 1) / (2 * count),
                _ => throw new ArgumentOutOfRangeException(nameof(spacing), spacing, null)
            };
        }
    }
}
