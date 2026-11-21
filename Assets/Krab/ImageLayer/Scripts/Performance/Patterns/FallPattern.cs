using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class FallPattern : ImagePattern
    {
        private const int MaxCount = 40;

        // Items travel from above the top edge to below the bottom edge, so they never pop in on screen.
        private const float StartY = -0.3f;
        private const float TravelY = 1.6f;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _speed;
        private readonly PrefsFloat _spin;
        private readonly PrefsBool _rise;
        private readonly PrefsInt _seed;

        public FallPattern() : base("Fall",
            "Spinning clips fall (or rise) through the screen at slightly different speeds. Each pass picks a " +
            "new place and clip.",
            "life", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 10);
            _size = new PrefsFloat(Key("Size"), 260);
            _speed = new PrefsFloat(Key("Speed"), 0.15f);
            _spin = new PrefsFloat(Key("Spin"), 45);
            _rise = new PrefsBool(Key("Rise"), false);
            _seed = new PrefsInt(Key("Seed"), 4);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size", 20f, 1080f),
                _speed.CreateSlider("Speed (screens per beat)", 0.01f, 1f),
                _spin.CreateSlider("Spin (degrees per beat)", 0f, 360f),
                _rise.CreateElement("Rise"),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float speed = _speed.Get();
            RequirePositive(speed, "Speed");

            int seed = _seed.Get();
            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                double itemSpeed = speed * (0.6 + 0.8 * Random01(seed, index, 0)) / TravelY;
                double travel = Random01(seed, index, 1) + context.Beats * itemSpeed;
                long lap = (long)Math.Floor(travel);
                float progress = (float)(travel - lap);
                float y = (StartY + TravelY * progress) * context.Height;
                if (_rise.Get()) y = context.Height - y;

                float x = (float)Random01(seed, index + 1000, lap) * context.Width;
                float scale = 0.6f + 0.6f * (float)Random01(seed, index + 2000, lap);
                float direction = Random01(seed, index, 2) < 0.5 ? -1f : 1f;
                float angle = (float)(Random01(seed, index, 3) * 360 + context.Beats * _spin.Get() * direction);
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
