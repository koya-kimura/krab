using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using UnityEngine;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TrailPattern : ImagePattern
    {
        private const int MaxCopies = 24;

        private readonly PatternParameter _clip;
        private readonly PatternParameter _size;
        private readonly PatternParameter _copies;
        private readonly PatternParameter _lag;
        private readonly PatternParameter _beatsPerLoop;
        private readonly PatternParameter _fade;

        public TrailPattern() : base("Trail",
            "One clip draws a figure-eight path; older copies follow behind it, smaller and fading.")
        {
            _clip = Int("Clip", 0, 0, 20);
            _size = Float("Size", 360, 40, 1080);
            _copies = Int("Copies", 8, 1, MaxCopies);
            _lag = Float("Lag", 0.4, 0.01, 1, "Lag (beats)");
            _beatsPerLoop = Float("Beats Per Loop", 16, 1, 64);
            _fade = Float("Fade", 0.9, 0, 1);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerLoop = values.GetFloat(_beatsPerLoop);
            RequirePositive(beatsPerLoop, "Beats Per Loop");

            var clip = set.Wrap(values.GetInt(_clip));
            int copies = Math.Clamp(values.GetInt(_copies), 1, MaxCopies);
            var canvas = context.Canvas;

            // Oldest first, so the leading copy is drawn on top.
            for (int index = copies - 1; index >= 0; index--)
            {
                double beats = context.Beats - index * values.GetFloat(_lag);
                double phase = beats / beatsPerLoop * Math.PI * 2;
                float x = context.Width * (0.5f + 0.36f * (float)Math.Sin(phase));
                float y = context.Height * (0.5f + 0.3f * (float)Math.Sin(phase * 2));
                float age = copies > 1 ? (float)index / copies : 0f;
                float itemSize = size * (1f - 0.4f * age);

                canvas.Tint(new Color(1f, 1f, 1f, 1f - values.GetFloat(_fade) * age));
                canvas.ImageFit(clip, Frame(clip, context, -index), x - itemSize / 2,
                    y - itemSize / 2, itemSize, itemSize);
            }
        }
    }
}
