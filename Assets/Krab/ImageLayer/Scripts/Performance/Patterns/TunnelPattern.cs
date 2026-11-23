using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using UnityEngine;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TunnelPattern : ImagePattern
    {
        private const int MaxLayers = 16;

        private readonly PatternParameter _layers;
        private readonly PatternParameter _size;
        private readonly PatternParameter _ratio;
        private readonly PatternParameter _beatsPerLayer;
        private readonly PatternParameter _twist;
        private readonly PatternParameter _seed;

        public TunnelPattern() : base("Tunnel",
            "Copies nested from the center, each larger than the last, zooming outward forever. Every layer " +
            "keeps its clip while it grows.")
        {
            _layers = Int("Layers", 7, 1, MaxLayers);
            _size = Float("Size", 60, 10, 400, "Size (innermost)");
            _ratio = Float("Ratio", 1.6, 1.05, 3);
            _beatsPerLayer = Float("Beats Per Layer", 1, 0.125, 8);
            _twist = Float("Twist", 0, -45, 45, "Twist (degrees per layer)");
            _seed = Seed(3);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerLayer = values.GetFloat(_beatsPerLayer);
            RequirePositive(beatsPerLayer, "Beats Per Layer");
            float ratio = values.GetFloat(_ratio);
            if (!(ratio > 1f)) throw new ArgumentException("Ratio must be greater than 1.");

            int layers = Math.Clamp(values.GetInt(_layers), 1, MaxLayers);
            double zoom = context.Beats / beatsPerLayer;
            long step = (long)Math.Floor(zoom);
            float fraction = (float)(zoom - step);
            var canvas = context.Canvas;

            // Largest first (drawn behind). A layer's id stays the same as it grows into the next slot.
            for (int layer = layers - 1; layer >= 0; layer--)
            {
                long id = layer - step;
                var clip = set.Wrap(RandomIndex(values.GetInt(_seed), 0, id, set.ClipCount));
                float itemSize = size * Mathf.Pow(ratio, layer + fraction);
                float alpha = layer == 0 ? fraction : 1f;

                canvas.Push();
                canvas.Translate(context.Width / 2, context.Height / 2);
                canvas.Rotate(id * values.GetFloat(_twist));
                canvas.Tint(new Color(1f, 1f, 1f, alpha));
                canvas.ImageFit(clip, Frame(clip, context, id * 5), -itemSize / 2, -itemSize / 2, itemSize, itemSize);
                canvas.Pop();
            }
        }
    }
}
