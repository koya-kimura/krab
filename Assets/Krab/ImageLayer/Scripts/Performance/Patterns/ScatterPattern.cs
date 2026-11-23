using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ScatterPattern : ImagePattern
    {
        private const int MaxCount = 40;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _beatsPerChange;
        private readonly PatternParameter _spin;
        private readonly PatternParameter _seed;

        public ScatterPattern() : base("Scatter",
            "Clips dropped at seeded random places and angles across the screen, re-rolled on the beat.")
        {
            _count = Int("Count", 8, 1, MaxCount);
            _size = Float("Size", 320, 20, 1080);
            _beatsPerChange = Float("Beats Per Change", 2, 0.25, 16);
            _spin = Float("Spin", 30, 0, 180, "Spin (± degrees)");
            _seed = Seed(2);
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
                int slot = index * 4;
                float x = (float)Random01(seed, slot, step) * context.Width;
                float y = (float)Random01(seed, slot + 1, step) * context.Height;
                float angle = (float)(Random01(seed, slot + 2, step) * 2 - 1) * values.GetFloat(_spin);
                var clip = set.Wrap(RandomIndex(seed, slot + 3, step, set.ClipCount));

                canvas.Push();
                canvas.Translate(x, y);
                canvas.Rotate(angle);
                canvas.ImageFit(clip, Frame(clip, context, index * 7), -size / 2, -size / 2, size, size);
                canvas.Pop();
            }
        }
    }
}
