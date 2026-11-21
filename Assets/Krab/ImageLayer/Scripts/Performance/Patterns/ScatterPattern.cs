using System;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ScatterPattern : ImagePattern
    {
        private const int MaxCount = 40;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsFloat _spin;
        private readonly PrefsInt _seed;

        public ScatterPattern() : base("Scatter",
            "Clips dropped at seeded random places and angles across the screen, re-rolled on the beat.",
            "life", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 8);
            _size = new PrefsFloat(Key("Size"), 320);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 2);
            _spin = new PrefsFloat(Key("Spin"), 30);
            _seed = new PrefsInt(Key("Seed"), 2);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size", 20f, 1080f),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.25f, 16f),
                _spin.CreateSlider("Spin (± degrees)", 0f, 180f),
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
                int slot = index * 4;
                float x = (float)Random01(seed, slot, step) * context.Width;
                float y = (float)Random01(seed, slot + 1, step) * context.Height;
                float angle = (float)(Random01(seed, slot + 2, step) * 2 - 1) * _spin.Get();
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
