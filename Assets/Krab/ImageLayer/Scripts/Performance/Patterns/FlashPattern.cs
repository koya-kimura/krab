using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class FlashPattern : ImagePattern
    {
        private const int MaxCount = 12;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _sizeSpread;
        private readonly PatternParameter _beatsPerChange;
        private readonly PatternParameter _flip;
        private readonly PatternParameter _seed;

        public FlashPattern() : base("Flash",
            "Hard cuts: on every step the clips jump to a new place, size and clip. Short steps make a strobe.")
        {
            _count = Int("Count", 2, 1, MaxCount);
            _size = Float("Size", 600, 40, 1500);
            _sizeSpread = Float("Size Spread", 0.5, 0, 1);
            _beatsPerChange = Float("Beats Per Change", 0.5, 0.125, 8);
            _flip = Bool("Random Flip", true);
            _seed = Seed(5);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerChange = values.GetFloat(_beatsPerChange);
            RequirePositive(beatsPerChange, "Beats Per Change");

            int seed = values.GetInt(_seed);
            long step = BeatSteps.Step(context.Beats, beatsPerChange);
            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                float scale = 1f - values.GetFloat(_sizeSpread) * (float)Random01(seed, index, step);
                float itemSize = size * scale;
                float x = (float)Random01(seed, index + 100, step) * context.Width;
                float y = (float)Random01(seed, index + 200, step) * context.Height;
                bool flip = values.GetBool(_flip) && Random01(seed, index + 300, step) < 0.5;
                var clip = set.Wrap(RandomIndex(seed, index + 400, step, set.ClipCount));

                canvas.Push();
                canvas.Translate(x, y);
                if (flip) canvas.Scale(-1f, 1f);
                canvas.ImageFit(clip, Frame(clip, context, index * 9), -itemSize / 2, -itemSize / 2, itemSize,
                    itemSize);
                canvas.Pop();
            }
        }
    }
}
