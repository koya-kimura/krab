using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class TrailPattern : ImagePattern
    {
        private const int MaxCopies = 24;

        private readonly PrefsInt _clip;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _copies;
        private readonly PrefsFloat _lag;
        private readonly PrefsFloat _beatsPerLoop;
        private readonly PrefsFloat _fade;

        public TrailPattern() : base("Trail",
            "One clip draws a figure-eight path; older copies follow behind it, smaller and fading.",
            "hand", ImageLook.Original)
        {
            _clip = new PrefsInt(Key("Clip"), 0);
            _size = new PrefsFloat(Key("Size"), 360);
            _copies = new PrefsInt(Key("Copies"), 8);
            _lag = new PrefsFloat(Key("Lag"), 0.4f);
            _beatsPerLoop = new PrefsFloat(Key("Beats Per Loop"), 16);
            _fade = new PrefsFloat(Key("Fade"), 0.9f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _clip.CreateSlider("Clip", 0, 20),
                _size.CreateSlider("Size", 40f, 1080f),
                _copies.CreateSlider("Copies", 1, MaxCopies),
                _lag.CreateSlider("Lag (beats)", 0.01f, 1f),
                _beatsPerLoop.CreateSlider("Beats Per Loop", 1f, 64f),
                _fade.CreateSlider("Fade", 0f, 1f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerLoop = _beatsPerLoop.Get();
            RequirePositive(beatsPerLoop, "Beats Per Loop");

            var clip = set.Wrap(_clip.Get());
            int copies = Math.Clamp(_copies.Get(), 1, MaxCopies);
            var canvas = context.Canvas;

            // Oldest first, so the leading copy is drawn on top.
            for (int index = copies - 1; index >= 0; index--)
            {
                double beats = context.Beats - index * _lag.Get();
                double phase = beats / beatsPerLoop * Math.PI * 2;
                float x = context.Width * (0.5f + 0.36f * (float)Math.Sin(phase));
                float y = context.Height * (0.5f + 0.3f * (float)Math.Sin(phase * 2));
                float age = copies > 1 ? (float)index / copies : 0f;
                float itemSize = size * (1f - 0.4f * age);

                canvas.Tint(new Color(1f, 1f, 1f, 1f - _fade.Get() * age));
                canvas.ImageFit(clip, Frame(clip, context, -index), x - itemSize / 2,
                    y - itemSize / 2, itemSize, itemSize);
            }
        }
    }
}
