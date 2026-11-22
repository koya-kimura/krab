using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ColumnsPattern : ImagePattern
    {
        private const int MaxColumns = 16;

        private readonly PrefsInt _columns;
        private readonly PrefsFloat _cellHeight;
        private readonly PrefsFloat _cellsPerBeat;
        private readonly PrefsFloat _fill;
        private readonly PrefsInt _seed;

        public ColumnsPattern() : base("Columns",
            "Vertical columns of mixed clips scrolling up and down in turn, like a departure board.",
            "building", ImageLook.Duotone)
        {
            _columns = new PrefsInt(Key("Columns"), 5);
            _cellHeight = new PrefsFloat(Key("Cell Height"), 300);
            _cellsPerBeat = new PrefsFloat(Key("Cells Per Beat"), 0.25f);
            _fill = new PrefsFloat(Key("Fill"), 0.9f);
            _seed = new PrefsInt(Key("Seed"), 2);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _columns.CreateSlider("Columns", 1, MaxColumns),
                _cellHeight.CreateSlider("Cell Height", 40f, 1080f),
                _cellsPerBeat.CreateSlider("Cells Per Beat", -2f, 2f),
                _fill.CreateSlider("Fill", 0.1f, 1f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float cellHeight = context.Px(_cellHeight.Get());
            RequirePositive(cellHeight, "Cell Height");

            int seed = _seed.Get();
            int columns = Math.Clamp(_columns.Get(), 1, MaxColumns);
            float cellWidth = context.Width / columns;
            float fill = Math.Clamp(_fill.Get(), 0.1f, 1f);
            float boxWidth = cellWidth * fill;
            float boxHeight = cellHeight * fill;
            int visibleCells = (int)Math.Ceiling(context.Height / cellHeight) + 1;
            for (int column = 0; column < columns; column++)
            {
                // Each cell keeps its clip while it scrolls: cells are numbered along the column, not the screen.
                double shift = context.Beats * _cellsPerBeat.Get() * (column % 2 == 0 ? 1 : -1);
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
