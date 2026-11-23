using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class SpiralPattern : ImagePattern
    {
        private const int MaxCount = 80;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _spread;
        private readonly PatternParameter _degreesBetween;
        private readonly PatternParameter _degreesPerBeat;
        private readonly PatternParameter _grow;

        public SpiralPattern() : base("Spiral",
            "Clips laid out on a sunflower-seed spiral from the center, growing toward the edge and turning " +
            "with the beat.")
        {
            _count = Int("Count", 40, 1, MaxCount);
            _size = Float("Size", 90, 10, 400, "Size (center)");
            _spread = Float("Spread", 80, 10, 200);
            _degreesBetween = Float("Degrees Between", 137.5, 0, 360);
            _degreesPerBeat = Float("Degrees Per Beat", 8, -90, 90);
            _grow = Float("Grow", 1.5, 0.2, 4, "Grow (edge size / center size)");
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float spread = context.Px(values.GetFloat(_spread));
            double turn = context.Beats * values.GetFloat(_degreesPerBeat);

            // Outermost first, so the small center copies stay on top.
            for (int index = count - 1; index >= 0; index--)
            {
                double radians = (turn + index * values.GetFloat(_degreesBetween)) * Math.PI / 180;
                float radius = spread * (float)Math.Sqrt(index);
                float x = context.Width / 2 + radius * (float)Math.Cos(radians);
                float y = context.Height / 2 + radius * (float)Math.Sin(radians);
                float t = count > 1 ? (float)index / (count - 1) : 0f;
                float itemSize = size * (1f + (values.GetFloat(_grow) - 1f) * t);
                var clip = set.Wrap(index);
                context.Canvas.ImageFit(clip, Frame(clip, context, index * 3), x - itemSize / 2, y - itemSize / 2,
                    itemSize, itemSize);
            }
        }
    }
}
