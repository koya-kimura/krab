using System;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class SpiralPattern : ImagePattern
    {
        private const int MaxCount = 80;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _spread;
        private readonly PrefsFloat _degreesBetween;
        private readonly PrefsFloat _degreesPerBeat;
        private readonly PrefsFloat _grow;

        public SpiralPattern() : base("Spiral",
            "Clips laid out on a sunflower-seed spiral from the center, growing toward the edge and turning " +
            "with the beat.",
            "animal", ImageLook.Threshold)
        {
            _count = new PrefsInt(Key("Count"), 40);
            _size = new PrefsFloat(Key("Size"), 90);
            _spread = new PrefsFloat(Key("Spread"), 80);
            _degreesBetween = new PrefsFloat(Key("Degrees Between"), 137.5f);
            _degreesPerBeat = new PrefsFloat(Key("Degrees Per Beat"), 8);
            _grow = new PrefsFloat(Key("Grow"), 1.5f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size (center)", 10f, 400f),
                _spread.CreateSlider("Spread", 10f, 200f),
                _degreesBetween.CreateSlider("Degrees Between", 0f, 360f),
                _degreesPerBeat.CreateSlider("Degrees Per Beat", -90f, 90f),
                _grow.CreateSlider("Grow (edge size / center size)", 0.2f, 4f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float spread = context.Px(_spread.Get());
            double turn = context.Beats * _degreesPerBeat.Get();

            // Outermost first, so the small center copies stay on top.
            for (int index = count - 1; index >= 0; index--)
            {
                double radians = (turn + index * _degreesBetween.Get()) * Math.PI / 180;
                float radius = spread * (float)Math.Sqrt(index);
                float x = context.Width / 2 + radius * (float)Math.Cos(radians);
                float y = context.Height / 2 + radius * (float)Math.Sin(radians);
                float t = count > 1 ? (float)index / (count - 1) : 0f;
                float itemSize = size * (1f + (_grow.Get() - 1f) * t);
                var clip = set.Wrap(index);
                context.Canvas.ImageFit(clip, Frame(clip, context, index * 3), x - itemSize / 2, y - itemSize / 2,
                    itemSize, itemSize);
            }
        }
    }
}
