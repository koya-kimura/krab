using System;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class OrbitPattern : ImagePattern
    {
        private const int MaxCount = 24;

        private readonly PatternParameter _count;
        private readonly PatternParameter _radius;
        private readonly PatternParameter _size;
        private readonly PatternParameter _degreesPerBeat;

        public OrbitPattern() : base("Orbit",
            "Upright clips evenly spaced on a circle around the center, turning with the beat.")
        {
            _count = Int("Count", 8, 1, MaxCount);
            _radius = Float("Radius", 360, 0, 1000);
            _size = Float("Size", 180, 20, 800);
            _degreesPerBeat = Float("Degrees Per Beat", 15, -90, 90);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float radius = context.Px(values.GetFloat(_radius));
            double turn = context.Beats * values.GetFloat(_degreesPerBeat);
            for (int index = 0; index < count; index++)
            {
                double radians = (turn + 360.0 * index / count) * Math.PI / 180;
                float x = context.Width / 2 + radius * (float)Math.Cos(radians);
                float y = context.Height / 2 + radius * (float)Math.Sin(radians);
                var clip = set.Wrap(index);
                context.Canvas.ImageFit(clip, Frame(clip, context, index * 5), x - size / 2, y - size / 2, size, size);
            }
        }
    }
}
