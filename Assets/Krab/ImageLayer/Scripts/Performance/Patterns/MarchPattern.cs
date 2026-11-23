using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class MarchPattern : ImagePattern
    {
        private const int MaxRows = 12;

        private readonly PatternParameter _clip;
        private readonly PatternParameter _size;
        private readonly PatternParameter _rows;
        private readonly PatternParameter _cellsPerBeat;
        private readonly PatternParameter _position;
        private readonly PatternParameter _alternate;

        public MarchPattern() : base("March",
            "One clip repeated side by side in rows that march across the screen. Rows can alternate direction.")
        {
            _clip = Int("Clip", 0, 0, 20);
            _size = Float("Size", 220, 40, 600);
            _rows = Int("Rows", 1, 1, MaxRows);
            _cellsPerBeat = Float("Cells Per Beat", 0.5, -2, 2);
            _position = Float("Position", 0.15, 0, 1, "Position (0 top .. 1 bottom)");
            _alternate = Bool("Alternate", true, "Alternate Direction");
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            var clip = set.Wrap(values.GetInt(_clip));
            int rows = Math.Clamp(values.GetInt(_rows), 1, MaxRows);
            float rowHeight = size * 1.1f;
            float top = context.Height * values.GetFloat(_position) - rows * rowHeight / 2;
            double shift = context.Beats * values.GetFloat(_cellsPerBeat);
            float fraction = (float)(shift - Math.Floor(shift));
            for (int row = 0; row < rows; row++)
            {
                float direction = values.GetBool(_alternate) && row % 2 == 1 ? -1f : 1f;
                float offset = fraction * size * direction;
                var frame = Frame(clip, context, row * 3);
                float y = top + row * rowHeight;
                for (float x = -2 * size; x < context.Width + size; x += size)
                    context.Canvas.ImageFit(clip, frame, x + offset, y, size, rowHeight, FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
