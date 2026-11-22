using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class RadialPattern : ImagePattern
    {
        private const int MaxCount = 32;

        private readonly PrefsInt _clip;
        private readonly PrefsInt _count;
        private readonly PrefsFloat _radius;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _degreesPerBeat;
        private readonly PrefsFloat _pulse;

        public RadialPattern() : base("Radial",
            "Copies of one clip around the center, each turned to face outward like a flower or a clock. The " +
            "ring turns and pulses on the beat.",
            "dothand", ImageLook.Duotone)
        {
            _clip = new PrefsInt(Key("Clip"), 0);
            _count = new PrefsInt(Key("Count"), 10);
            _radius = new PrefsFloat(Key("Radius"), 260);
            _size = new PrefsFloat(Key("Size"), 260);
            _degreesPerBeat = new PrefsFloat(Key("Degrees Per Beat"), -10);
            _pulse = new PrefsFloat(Key("Pulse"), 0.15f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _clip.CreateSlider("Clip", 0, 20),
                _count.CreateSlider("Count", 1, MaxCount),
                _radius.CreateSlider("Radius", 0f, 800f),
                _size.CreateSlider("Size", 20f, 800f),
                _degreesPerBeat.CreateSlider("Degrees Per Beat", -90f, 90f),
                _pulse.CreateSlider("Pulse", 0f, 1f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            var clip = set.Wrap(_clip.Get());
            int count = Math.Clamp(_count.Get(), 1, MaxCount);

            // Pushes out on each beat and settles back before the next.
            double beatPhase = context.Beats - Math.Floor(context.Beats);
            float pulse = 1f + _pulse.Get() * (float)Math.Pow(1 - beatPhase, 3);
            float radius = context.Px(_radius.Get()) * pulse;
            double turn = context.Beats * _degreesPerBeat.Get();
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                float angle = (float)(turn + 360.0 * index / count);
                canvas.Push();
                canvas.Translate(context.Width / 2, context.Height / 2);
                canvas.Rotate(angle);
                canvas.Translate(0f, -radius);
                canvas.ImageFit(clip, Frame(clip, context), -size / 2, -size, size, size,
                    FitMode.Contain, 0.5f, 1f);
                canvas.Pop();
            }
        }
    }
}
