using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class RadialPattern : ImagePattern
    {
        private const int MaxCount = 32;

        private readonly PatternParameter _clip;
        private readonly PatternParameter _count;
        private readonly PatternParameter _radius;
        private readonly PatternParameter _size;
        private readonly PatternParameter _degreesPerBeat;
        private readonly PatternParameter _pulse;

        public RadialPattern() : base("Radial",
            "Copies of one clip around the center, each turned to face outward like a flower or a clock. The " +
            "ring turns and pulses on the beat.")
        {
            _clip = Int("Clip", 0, 0, 20);
            _count = Int("Count", 10, 1, MaxCount);
            _radius = Float("Radius", 260, 0, 800);
            _size = Float("Size", 260, 20, 800);
            _degreesPerBeat = Float("Degrees Per Beat", -10, -90, 90);
            _pulse = Float("Pulse", 0.15, 0, 1);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            var clip = set.Wrap(values.GetInt(_clip));
            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);

            // Pushes out on each beat and settles back before the next.
            double beatPhase = context.Beats - Math.Floor(context.Beats);
            float pulse = 1f + values.GetFloat(_pulse) * (float)Math.Pow(1 - beatPhase, 3);
            float radius = context.Px(values.GetFloat(_radius)) * pulse;
            double turn = context.Beats * values.GetFloat(_degreesPerBeat);
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
