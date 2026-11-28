using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class PilePattern : ImagePattern
    {
        private const int MaxCount = 60;
        private const float PopScale = 0.35f;

        private readonly PatternParameter _maxCount;
        private readonly PatternParameter _size;
        private readonly PatternParameter _beatsPerItem;
        private readonly PatternParameter _tilt;
        private readonly PatternParameter _seed;

        public PilePattern() : base("Pile",
            "One clip lands per step at a random place and angle, piling up until the pile is full, then " +
            "the screen clears and it starts again.")
        {
            _maxCount = Int("Max Count", 16, 1, MaxCount);
            _size = Float("Size", 320, 40, 1000);
            _beatsPerItem = Float("Beats Per Item", 1, 0.125, 8);
            _tilt = Float("Tilt", 30, 0, 180, "Tilt (max degrees)");
            _seed = Seed(8);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerItem = values.GetFloat(_beatsPerItem);
            RequirePositive(beatsPerItem, "Beats Per Item");

            int seed = values.GetInt(_seed);
            int maxCount = Math.Clamp(values.GetInt(_maxCount), 1, MaxCount);
            double position = context.Beats / beatsPerItem;
            long step = (long)Math.Floor(position);
            float progress = (float)(position - step);
            long round = Math.DivRem(step, maxCount, out long placed);
            if (placed < 0)
            {
                placed += maxCount;
                round--;
            }

            var canvas = context.Canvas;
            for (int index = 0; index <= placed; index++)
            {
                float x = (float)(0.1 + 0.8 * Random01(seed, index, round)) * context.Width;
                float y = (float)(0.1 + 0.8 * Random01(seed, index + 100, round)) * context.Height;
                float angle = (float)((Random01(seed, index + 200, round) * 2 - 1) * values.GetFloat(_tilt));
                var clip = set.Wrap(RandomIndex(seed, index + 300, round, set.ClipCount));

                // The newest item drops in from slightly larger, easing out over its step.
                float ease = 1f - (1f - progress) * (1f - progress);
                float itemSize = size * (index == placed ? 1f + PopScale * (1f - ease) : 1f);

                canvas.Push();
                canvas.Translate(x, y);
                canvas.Rotate(angle);
                canvas.ImageFit(clip, Frame(clip, context, index * 7), -itemSize / 2, -itemSize / 2, itemSize,
                    itemSize);
                canvas.Pop();
            }
        }
    }
}
