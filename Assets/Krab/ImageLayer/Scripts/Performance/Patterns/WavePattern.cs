using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class WavePattern : ImagePattern
    {
        private const int MaxCount = 24;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _amplitude;
        private readonly PrefsFloat _beatsPerWave;
        private readonly PrefsFloat _phaseStep;
        private readonly PrefsFloat _position;

        public WavePattern() : base("Wave",
            "A row of clips across the screen that rise and fall one after another, like a stadium wave.",
            "dance", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 8);
            _size = new PrefsFloat(Key("Size"), 300);
            _amplitude = new PrefsFloat(Key("Amplitude"), 160);
            _beatsPerWave = new PrefsFloat(Key("Beats Per Wave"), 2);
            _phaseStep = new PrefsFloat(Key("Phase Step"), 40);
            _position = new PrefsFloat(Key("Position"), 0.55f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size", 40f, 800f),
                _amplitude.CreateSlider("Amplitude", 0f, 500f),
                _beatsPerWave.CreateSlider("Beats Per Wave", 0.25f, 16f),
                _phaseStep.CreateSlider("Phase Step (degrees between neighbors)", -180f, 180f),
                _position.CreateSlider("Position (0 top .. 1 bottom)", 0f, 1f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");
            float beatsPerWave = _beatsPerWave.Get();
            RequirePositive(beatsPerWave, "Beats Per Wave");

            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float cellWidth = context.Width / count;
            float amplitude = context.Px(_amplitude.Get());
            double phase = context.Beats / beatsPerWave * Math.PI * 2;
            double step = _phaseStep.Get() * Math.PI / 180;
            for (int index = 0; index < count; index++)
            {
                var clip = set.Wrap(index);
                float lift = amplitude * (float)Math.Sin(phase - index * step);
                float top = context.Height * _position.Get() - size / 2 - lift;
                context.Canvas.ImageFit(clip, Frame(clip, context, index * 4), cellWidth * index, top, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
