using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class ParadePattern : ImagePattern
    {
        private const int MaxCount = 30;
        private const float FarScale = 0.35f;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _speed;
        private readonly PatternParameter _horizon;
        private readonly PatternParameter _bothWays;
        private readonly PatternParameter _facesLeft;
        private readonly PatternParameter _seed;

        public ParadePattern() : base("Parade",
            "A crowd walking across the screen at different depths: far walkers are small, high and slow, near " +
            "ones large, low and fast.")
        {
            _count = Int("Count", 12, 1, MaxCount);
            _size = Float("Size", 520, 40, 1080, "Size (nearest)");
            _speed = Float("Speed", 120, 0, 600, "Speed (pixels per beat, nearest)");
            _horizon = Float("Horizon", 0.45, 0, 1, "Horizon (0 top .. 1 bottom)");
            _bothWays = Bool("Both Ways", true);
            _facesLeft = Bool("Clips Face Left", false);
            _seed = Seed(6);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            int seed = values.GetInt(_seed);
            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float horizon = context.Height * values.GetFloat(_horizon);
            var canvas = context.Canvas;

            // Index order is depth order (far to near), so near walkers are drawn on top without sorting.
            for (int index = 0; index < count; index++)
            {
                float depth = (float)((index + Random01(seed, index, 0)) / count);
                float scale = FarScale + (1f - FarScale) * depth;
                float itemSize = size * scale;
                float feet = horizon + (context.Height - horizon) * depth;
                bool left = values.GetBool(_bothWays) && Random01(seed, index, 1) < 0.5;

                float span = context.Width + itemSize * 2;
                double travel = Random01(seed, index, 2) * span + context.Beats * context.Px(values.GetFloat(_speed)) * scale;
                float x = (float)(travel % span) - itemSize;
                if (left) x = context.Width - x;

                var clip = set.Wrap(RandomIndex(seed, index, 3, set.ClipCount));
                canvas.Push();
                canvas.Translate(x, feet);
                if (left != values.GetBool(_facesLeft)) canvas.Scale(-1f, 1f);
                canvas.ImageFit(clip, Frame(clip, context, index * 11), -itemSize / 2, -itemSize, itemSize, itemSize,
                    FitMode.Contain, 0.5f, 1f);
                canvas.Pop();
            }
        }
    }
}
