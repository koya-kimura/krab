using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ColumnsPattern : ImagePattern
    {
        private const int MaxColumns = 16;

        private readonly PatternParameter _columns;
        private readonly PatternParameter _cellHeight;
        private readonly PatternParameter _cellsPerBeat;
        private readonly PatternParameter _fill;
        private readonly PatternParameter _seed;

        public ColumnsPattern() : base("Columns",
            "Vertical columns of mixed clips scrolling up and down in turn, like a departure board.")
        {
            _columns = Int("Columns", 5, 1, MaxColumns);
            _cellHeight = Float("Cell Height", 300, 40, 1080);
            _cellsPerBeat = Float("Cells Per Beat", 0.25, -2, 2);
            _fill = Float("Fill", 0.9, 0.1, 1);
            _seed = Seed(2);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float cellHeight = context.Px(values.GetFloat(_cellHeight));
            RequirePositive(cellHeight, "Cell Height");

            int seed = values.GetInt(_seed);
            int columns = Math.Clamp(values.GetInt(_columns), 1, MaxColumns);
            float cellWidth = context.Width / columns;
            float fill = Math.Clamp(values.GetFloat(_fill), 0.1f, 1f);
            float boxWidth = cellWidth * fill;
            float boxHeight = cellHeight * fill;
            int visibleCells = (int)Math.Ceiling(context.Height / cellHeight) + 1;
            for (int column = 0; column < columns; column++)
            {
                // Each cell keeps its clip while it scrolls: cells are numbered along the column, not the screen.
                double shift = context.Beats * values.GetFloat(_cellsPerBeat) * (column % 2 == 0 ? 1 : -1);
                long first = (long)Math.Floor(-shift);
                for (long cell = first; cell <= first + visibleCells; cell++)
                {
                    float y = (float)((cell + shift) * cellHeight) + (cellHeight - boxHeight) / 2;
                    float x = cellWidth * column + (cellWidth - boxWidth) / 2;
                    var clip = set.Wrap(RandomIndex(seed, column, cell, set.ClipCount));
                    context.Canvas.ImageFit(clip, Frame(clip, context, cell * 5), x, y, boxWidth, boxHeight);
                }
            }
        }
    }
}
