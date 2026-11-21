using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class MarchPattern : ImagePattern
    {
        private const int MaxRows = 12;

        private readonly PrefsInt _clip;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _rows;
        private readonly PrefsFloat _cellsPerBeat;
        private readonly PrefsFloat _position;
        private readonly PrefsBool _alternate;

        public MarchPattern() : base("March",
            "One clip repeated side by side in rows that march across the screen. Rows can alternate direction.",
            "animal", ImageLook.Threshold, true)
        {
            _clip = new PrefsInt(Key("Clip"), 0);
            _size = new PrefsFloat(Key("Size"), 220);
            _rows = new PrefsInt(Key("Rows"), 1);
            _cellsPerBeat = new PrefsFloat(Key("Cells Per Beat"), 0.5f);
            _position = new PrefsFloat(Key("Position"), 0.15f);
            _alternate = new PrefsBool(Key("Alternate"), true);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _clip.CreateSlider("Clip", 0, 20),
                _size.CreateSlider("Size", 40f, 600f),
                _rows.CreateSlider("Rows", 1, MaxRows),
                _cellsPerBeat.CreateSlider("Cells Per Beat", -2f, 2f),
                _position.CreateSlider("Position (0 top .. 1 bottom)", 0f, 1f),
                _alternate.CreateElement("Alternate Direction"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            var clip = set.Wrap(_clip.Get());
            int rows = Math.Clamp(_rows.Get(), 1, MaxRows);
            float rowHeight = size * 1.1f;
            float top = context.Height * _position.Get() - rows * rowHeight / 2;
            double shift = context.Beats * _cellsPerBeat.Get();
            float fraction = (float)(shift - Math.Floor(shift));
            for (int row = 0; row < rows; row++)
            {
                float direction = _alternate.Get() && row % 2 == 1 ? -1f : 1f;
                float offset = fraction * size * direction;
                var frame = Frame(clip, context, row * 3);
                float y = top + row * rowHeight;
                for (float x = -2 * size; x < context.Width + size; x += size)
                    context.Canvas.ImageFit(clip, frame, x + offset, y, size, rowHeight, FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
