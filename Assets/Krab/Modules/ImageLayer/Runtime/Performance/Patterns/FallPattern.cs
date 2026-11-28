using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class FallPattern : ImagePattern
    {
        private const int MaxCount = 40;

        // Items travel from above the top edge to below the bottom edge, so they never pop in on screen.
        private const float StartY = -0.3f;
        private const float TravelY = 1.6f;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _speed;
        private readonly PatternParameter _spin;
        private readonly PatternParameter _rise;
        private readonly PatternParameter _seed;

        public FallPattern() : base("Fall",
            "Spinning clips fall (or rise) through the screen at slightly different speeds. Each pass picks a " +
            "new place and clip.")
        {
            _count = Int("Count", 10, 1, MaxCount);
            _size = Float("Size", 260, 20, 1080);
            _speed = Float("Speed", 0.15, 0.01, 1, "Speed (screens per beat)");
            _spin = Float("Spin", 45, 0, 360, "Spin (degrees per beat)");
            _rise = Bool("Rise", false);
            _seed = Seed(4);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float speed = values.GetFloat(_speed);
            RequirePositive(speed, "Speed");

            int seed = values.GetInt(_seed);
            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                double itemSpeed = speed * (0.6 + 0.8 * Random01(seed, index, 0)) / TravelY;
                double travel = Random01(seed, index, 1) + context.Beats * itemSpeed;
                long lap = (long)Math.Floor(travel);
                float progress = (float)(travel - lap);
                float y = (StartY + TravelY * progress) * context.Height;
                if (values.GetBool(_rise)) y = context.Height - y;

                float x = (float)Random01(seed, index + 1000, lap) * context.Width;
                float scale = 0.6f + 0.6f * (float)Random01(seed, index + 2000, lap);
                float direction = Random01(seed, index, 2) < 0.5 ? -1f : 1f;
                float angle = (float)(Random01(seed, index, 3) * 360 + context.Beats * values.GetFloat(_spin) * direction);
                var clip = set.Wrap(RandomIndex(seed, index + 3000, lap, set.ClipCount));
                float itemSize = size * scale;

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
