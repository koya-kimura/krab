using System;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;

namespace Krab.TypeLayout.Core.Distribution
{
    /// <summary>One slot at the center of every cell of an evenly divided rectangle. Slots read rightward.</summary>
    public static class GridDistributor
    {
        public static LayoutSlot[] Distribute(Rect2 area, int columns, int rows, GridOrder order = GridOrder.RowMajor)
        {
            Require.Positive(columns, nameof(columns));
            Require.Positive(rows, nameof(rows));

            var slots = new LayoutSlot[columns * rows];
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var index = order == GridOrder.RowMajor ? row * columns + column : column * rows + row;
                    slots[index] = new LayoutSlot(CellCenter(area, columns, rows, column, row), 0);
                }
            }

            return slots;
        }

        public static Rect2 Cell(Rect2 area, int columns, int rows, int column, int row)
        {
            Require.Positive(columns, nameof(columns));
            Require.Positive(rows, nameof(rows));
            if (column < 0 || column >= columns) throw new ArgumentOutOfRangeException(nameof(column), column, null);
            if (row < 0 || row >= rows) throw new ArgumentOutOfRangeException(nameof(row), row, null);

            var left = area.Left + area.Width * column / columns;
            var top = area.Top + area.Height * row / rows;
            var right = area.Left + area.Width * (column + 1) / columns;
            var bottom = area.Top + area.Height * (row + 1) / rows;
            return Rect2.FromEdges(left, top, right, bottom);
        }

        private static Point2 CellCenter(Rect2 area, int columns, int rows, int column, int row)
        {
            return new Point2(area.Left + area.Width * (2 * column + 1) / (2 * columns),
                area.Top + area.Height * (2 * row + 1) / (2 * rows));
        }
    }
}
