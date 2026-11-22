using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TunnelPattern : ImagePattern
    {
        private const int MaxLayers = 16;

        private readonly PrefsInt _layers;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _ratio;
        private readonly PrefsFloat _beatsPerLayer;
        private readonly PrefsFloat _twist;
        private readonly PrefsInt _seed;

        public TunnelPattern() : base("Tunnel",
            "Copies nested from the center, each larger than the last, zooming outward forever. Every layer " +
            "keeps its clip while it grows.",
            "noface", ImageLook.Duotone)
        {
            _layers = new PrefsInt(Key("Layers"), 7);
            _size = new PrefsFloat(Key("Size"), 60);
            _ratio = new PrefsFloat(Key("Ratio"), 1.6f);
            _beatsPerLayer = new PrefsFloat(Key("Beats Per Layer"), 1);
            _twist = new PrefsFloat(Key("Twist"), 0);
            _seed = new PrefsInt(Key("Seed"), 3);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _layers.CreateSlider("Layers", 1, MaxLayers),
                _size.CreateSlider("Size (innermost)", 10f, 400f),
                _ratio.CreateSlider("Ratio", 1.05f, 3f),
                _beatsPerLayer.CreateSlider("Beats Per Layer", 0.125f, 8f),
                _twist.CreateSlider("Twist (degrees per layer)", -45f, 45f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerLayer = _beatsPerLayer.Get();
            RequirePositive(beatsPerLayer, "Beats Per Layer");
            float ratio = _ratio.Get();
            if (!(ratio > 1f)) throw new ArgumentException("Ratio must be greater than 1.");

            int layers = Math.Clamp(_layers.Get(), 1, MaxLayers);
            double zoom = context.Beats / beatsPerLayer;
            long step = (long)Math.Floor(zoom);
            float fraction = (float)(zoom - step);
            var canvas = context.Canvas;

            // Largest first (drawn behind). A layer's id stays the same as it grows into the next slot.
            for (int layer = layers - 1; layer >= 0; layer--)
            {
                long id = layer - step;
                var clip = set.Wrap(RandomIndex(_seed.Get(), 0, id, set.ClipCount));
                float itemSize = size * Mathf.Pow(ratio, layer + fraction);
                float alpha = layer == 0 ? fraction : 1f;

                canvas.Push();
                canvas.Translate(context.Width / 2, context.Height / 2);
                canvas.Rotate(id * _twist.Get());
                canvas.Tint(new Color(1f, 1f, 1f, alpha));
                canvas.ImageFit(clip, Frame(clip, context, id * 5), -itemSize / 2, -itemSize / 2, itemSize, itemSize);
                canvas.Pop();
            }
        }
    }
}
