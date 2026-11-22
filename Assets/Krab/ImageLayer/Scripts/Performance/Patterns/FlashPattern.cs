using System;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class FlashPattern : ImagePattern
    {
        private const int MaxCount = 12;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _sizeSpread;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsBool _flip;
        private readonly PrefsInt _seed;

        public FlashPattern() : base("Flash",
            "Hard cuts: on every step the clips jump to a new place, size and clip. Short steps make a strobe.",
            "hand", ImageLook.Threshold)
        {
            _count = new PrefsInt(Key("Count"), 2);
            _size = new PrefsFloat(Key("Size"), 600);
            _sizeSpread = new PrefsFloat(Key("Size Spread"), 0.5f);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 0.5f);
            _flip = new PrefsBool(Key("Random Flip"), true);
            _seed = new PrefsInt(Key("Seed"), 5);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size", 40f, 1500f),
                _sizeSpread.CreateSlider("Size Spread", 0f, 1f),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.125f, 8f),
                _flip.CreateElement("Random Flip"),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerChange = _beatsPerChange.Get();
            RequirePositive(beatsPerChange, "Beats Per Change");

            int seed = _seed.Get();
            long step = BeatSteps.Step(context.Beats, beatsPerChange);
            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                float scale = 1f - _sizeSpread.Get() * (float)Random01(seed, index, step);
                float itemSize = size * scale;
                float x = (float)Random01(seed, index + 100, step) * context.Width;
                float y = (float)Random01(seed, index + 200, step) * context.Height;
                bool flip = _flip.Get() && Random01(seed, index + 300, step) < 0.5;
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
