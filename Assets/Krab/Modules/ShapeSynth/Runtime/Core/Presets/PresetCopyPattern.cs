using System;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Which copies get their bound motion reversed (Depth × -1).</summary>
    public enum PresetFlip
    {
        None,
        Alternate,
        Column,
        Row,
        Checker
    }

    /// <summary>How a Shape's copies are staggered in time by the Layout Delay.</summary>
    public enum PresetDelayOrder
    {
        Index,
        Reverse,
        CenterOut,
        Column,
        Row,
        Random
    }

    /// <summary>
    /// Per-copy values derived from a copy's place in its Layout. Index-based patterns are fixed at compile
    /// time; Random delays are resolved per Voice seed and placement by the Core random resolver.
    /// </summary>
    public static class PresetCopyPattern
    {
        /// <summary>Column and row of a copy: Row/Circle count columns, Column counts rows, Grid wraps.</summary>
        public static (int Column, int Row) Cell(PresetLayout layout, int index)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            switch (layout.Kind)
            {
                case PresetLayoutKind.Column:
                    return (0, index);
                case PresetLayoutKind.Grid:
                    PresetLayout.RequireColumns(layout.Columns);
                    return (index % layout.Columns, index / layout.Columns);
                default:
                    return (index, 0);
            }
        }

        /// <summary>+1, or -1 for the copies the Flip pattern reverses.</summary>
        public static double FlipSign(PresetFlip flip, PresetLayout layout, int index)
        {
            var (column, row) = Cell(layout, index);
            switch (flip)
            {
                case PresetFlip.None:
                    return 1;
                case PresetFlip.Alternate:
                    return Odd(index);
                case PresetFlip.Column:
                    return Odd(column);
                case PresetFlip.Row:
                    return Odd(row);
                case PresetFlip.Checker:
                    return Odd(column + row);
                default:
                    throw new ArgumentException("Unknown Flip pattern.");
            }
        }

        /// <summary>Fixed delay of a copy in beats: Delay × its order value. Random is resolved separately.</summary>
        public static double DelayBeats(PresetLayout layout, int index, int count)
        {
            if (count <= 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));

            var (column, row) = Cell(layout, index);
            switch (layout.DelayOrder)
            {
                case PresetDelayOrder.Index:
                    return layout.Delay * index;
                case PresetDelayOrder.Reverse:
                    return layout.Delay * (count - 1 - index);
                case PresetDelayOrder.CenterOut:
                    return layout.Delay * Math.Abs(index - (count - 1) / 2.0);
                case PresetDelayOrder.Column:
                    return layout.Delay * column;
                case PresetDelayOrder.Row:
                    return layout.Delay * row;
                case PresetDelayOrder.Random:
                    return 0;
                default:
                    throw new ArgumentException("Unknown Delay order.");
            }
        }

        /// <summary>Longest delay any copy can get, for preview loop length.</summary>
        public static double MaxDelayBeats(PresetLayout layout)
        {
            if (layout.DelayOrder == PresetDelayOrder.Random) return layout.Delay;

            var count = layout.GetCopyCount();
            var max = 0.0;
            for (var i = 0; i < count; i++)
            {
                max = Math.Max(max, DelayBeats(layout, i, count));
            }

            return max;
        }

        private static double Odd(int value) => (value & 1) == 0 ? 1 : -1;
    }
}
