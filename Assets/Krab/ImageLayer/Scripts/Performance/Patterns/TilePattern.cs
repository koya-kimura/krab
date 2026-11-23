using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TilePattern : ImagePattern
    {
        private const int MaxCells = 16;

        private readonly PatternParameter _columns;
        private readonly PatternParameter _rows;
        private readonly PatternParameter _beatsPerChange;
        private readonly PatternParameter _fill;
        private readonly PatternParameter _seed;

        public TilePattern() : base("Tile",
            "A grid of cells, each showing a seeded random clip of the set. The picks change on the beat.")
        {
            _columns = Int("Columns", 6, 1, MaxCells);
            _rows = Int("Rows", 3, 1, MaxCells);
            _beatsPerChange = Float("Beats Per Change", 1, 0.25, 16);
            _fill = Float("Fill", 0.9, 0.1, 1.5);
            _seed = Seed(1);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            int columns = Math.Clamp(values.GetInt(_columns), 1, MaxCells);
            int rows = Math.Clamp(values.GetInt(_rows), 1, MaxCells);
            float beatsPerChange = values.GetFloat(_beatsPerChange);
            RequirePositive(beatsPerChange, "Beats Per Change");

            long step = BeatSteps.Step(context.Beats, beatsPerChange);
            float cellWidth = context.Width / columns;
            float cellHeight = context.Height / rows;
            float fill = Math.Max(0.01f, values.GetFloat(_fill));
            float boxWidth = cellWidth * fill;
            float boxHeight = cellHeight * fill;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                int cell = row * columns + column;
                var clip = set.Wrap(RandomIndex(values.GetInt(_seed), cell, step, set.ClipCount));
                float x = cellWidth * column + (cellWidth - boxWidth) / 2;
                float y = cellHeight * row + (cellHeight - boxHeight) / 2;
                context.Canvas.ImageFit(clip, Frame(clip, context, cell * 5), x, y, boxWidth, boxHeight);
            }
        }
    }
}
