using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class OrbitPattern : ImagePattern
    {
        private const int MaxCount = 24;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _radius;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _degreesPerBeat;

        public OrbitPattern() : base("Orbit",
            "Upright clips evenly spaced on a circle around the center, turning with the beat.",
            "life", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 8);
            _radius = new PrefsFloat(Key("Radius"), 360);
            _size = new PrefsFloat(Key("Size"), 180);
            _degreesPerBeat = new PrefsFloat(Key("Degrees Per Beat"), 15);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _radius.CreateSlider("Radius", 0f, 1000f),
                _size.CreateSlider("Size", 20f, 800f),
                _degreesPerBeat.CreateSlider("Degrees Per Beat", -90f, 90f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float radius = context.Px(_radius.Get());
            double turn = context.Beats * _degreesPerBeat.Get();
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
