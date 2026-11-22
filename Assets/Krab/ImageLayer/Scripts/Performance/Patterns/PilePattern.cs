using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class PilePattern : ImagePattern
    {
        private const int MaxCount = 60;
        private const float PopScale = 0.35f;

        private readonly PrefsInt _maxCount;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _beatsPerItem;
        private readonly PrefsFloat _tilt;
        private readonly PrefsInt _seed;

        public PilePattern() : base("Pile",
            "One clip lands per step at a random place and angle, piling up until the pile is full, then " +
            "the screen clears and it starts again.",
            "life", ImageLook.Original)
        {
            _maxCount = new PrefsInt(Key("Max Count"), 16);
            _size = new PrefsFloat(Key("Size"), 320);
            _beatsPerItem = new PrefsFloat(Key("Beats Per Item"), 1);
            _tilt = new PrefsFloat(Key("Tilt"), 30);
            _seed = new PrefsInt(Key("Seed"), 8);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _maxCount.CreateSlider("Max Count", 1, MaxCount),
                _size.CreateSlider("Size", 40f, 1000f),
                _beatsPerItem.CreateSlider("Beats Per Item", 0.125f, 8f),
                _tilt.CreateSlider("Tilt (max degrees)", 0f, 180f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerItem = _beatsPerItem.Get();
            RequirePositive(beatsPerItem, "Beats Per Item");

            int seed = _seed.Get();
            int maxCount = Math.Clamp(_maxCount.Get(), 1, MaxCount);
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
                float angle = (float)((Random01(seed, index + 200, round) * 2 - 1) * _tilt.Get());
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
