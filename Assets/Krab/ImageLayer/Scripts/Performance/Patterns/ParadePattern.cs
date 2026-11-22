using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ParadePattern : ImagePattern
    {
        private const int MaxCount = 30;
        private const float FarScale = 0.35f;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _speed;
        private readonly PrefsFloat _horizon;
        private readonly PrefsBool _bothWays;
        private readonly PrefsBool _facesLeft;
        private readonly PrefsInt _seed;

        public ParadePattern() : base("Parade",
            "A crowd walking across the screen at different depths: far walkers are small, high and slow, near " +
            "ones large, low and fast.",
            "walk", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 12);
            _size = new PrefsFloat(Key("Size"), 520);
            _speed = new PrefsFloat(Key("Speed"), 120);
            _horizon = new PrefsFloat(Key("Horizon"), 0.45f);
            _bothWays = new PrefsBool(Key("Both Ways"), true);
            _facesLeft = new PrefsBool(Key("Clips Face Left"), false);
            _seed = new PrefsInt(Key("Seed"), 6);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size (nearest)", 40f, 1080f),
                _speed.CreateSlider("Speed (pixels per beat, nearest)", 0f, 600f),
                _horizon.CreateSlider("Horizon (0 top .. 1 bottom)", 0f, 1f),
                _bothWays.CreateElement("Both Ways"),
                _facesLeft.CreateElement("Clips Face Left"),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            int seed = _seed.Get();
            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float horizon = context.Height * _horizon.Get();
            var canvas = context.Canvas;

            // Index order is depth order (far to near), so near walkers are drawn on top without sorting.
            for (int index = 0; index < count; index++)
            {
                float depth = (float)((index + Random01(seed, index, 0)) / count);
                float scale = FarScale + (1f - FarScale) * depth;
                float itemSize = size * scale;
                float feet = horizon + (context.Height - horizon) * depth;
                bool left = _bothWays.Get() && Random01(seed, index, 1) < 0.5;

                float span = context.Width + itemSize * 2;
                double travel = Random01(seed, index, 2) * span + context.Beats * context.Px(_speed.Get()) * scale;
                float x = (float)(travel % span) - itemSize;
                if (left) x = context.Width - x;

                var clip = set.Wrap(RandomIndex(seed, index, 3, set.ClipCount));
                canvas.Push();
                canvas.Translate(x, feet);
                if (left != _facesLeft.Get()) canvas.Scale(-1f, 1f);
                canvas.ImageFit(clip, Frame(clip, context, index * 11), -itemSize / 2, -itemSize, itemSize, itemSize,
                    FitMode.Contain, 0.5f, 1f);
                canvas.Pop();
            }
        }
    }
}
