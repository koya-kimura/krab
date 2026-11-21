using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TilePattern : ImagePattern
    {
        private const int MaxCells = 16;

        private readonly PrefsInt _columns;
        private readonly PrefsInt _rows;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsFloat _fill;
        private readonly PrefsInt _seed;

        public TilePattern() : base("Tile",
            "A grid of cells, each showing a seeded random clip of the set. The picks change on the beat.",
            "noface", ImageLook.Duotone)
        {
            _columns = new PrefsInt(Key("Columns"), 6);
            _rows = new PrefsInt(Key("Rows"), 3);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 1);
            _fill = new PrefsFloat(Key("Fill"), 0.9f);
            _seed = new PrefsInt(Key("Seed"), 1);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _columns.CreateSlider("Columns", 1, MaxCells),
                _rows.CreateSlider("Rows", 1, MaxCells),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.25f, 16f),
                _fill.CreateSlider("Fill", 0.1f, 1.5f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            int columns = Math.Clamp(_columns.Get(), 1, MaxCells);
            int rows = Math.Clamp(_rows.Get(), 1, MaxCells);
            float beatsPerChange = _beatsPerChange.Get();
            RequirePositive(beatsPerChange, "Beats Per Change");

            long step = BeatSteps.Step(context.Beats, beatsPerChange);
            float cellWidth = context.Width / columns;
            float cellHeight = context.Height / rows;
            float fill = Math.Max(0.01f, _fill.Get());
            float boxWidth = cellWidth * fill;
            float boxHeight = cellHeight * fill;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                int cell = row * columns + column;
                var clip = set.Wrap(RandomIndex(_seed.Get(), cell, step, set.ClipCount));
                float x = cellWidth * column + (cellWidth - boxWidth) / 2;
                float y = cellHeight * row + (cellHeight - boxHeight) / 2;
                context.Canvas.ImageFit(clip, Frame(clip, context, cell * 5), x, y, boxWidth, boxHeight);
            }
        }
    }
}
