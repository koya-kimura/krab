using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class WavePattern : ImagePattern
    {
        private const int MaxCount = 24;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _amplitude;
        private readonly PatternParameter _beatsPerWave;
        private readonly PatternParameter _phaseStep;
        private readonly PatternParameter _position;

        public WavePattern() : base("Wave",
            "A row of clips across the screen that rise and fall one after another, like a stadium wave.")
        {
            _count = Int("Count", 8, 1, MaxCount);
            _size = Float("Size", 300, 40, 800);
            _amplitude = Float("Amplitude", 160, 0, 500);
            _beatsPerWave = Float("Beats Per Wave", 2, 0.25, 16);
            _phaseStep = Float("Phase Step", 40, -180, 180, "Phase Step (degrees between neighbors)");
            _position = Float("Position", 0.55, 0, 1, "Position (0 top .. 1 bottom)");
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerWave = values.GetFloat(_beatsPerWave);
            RequirePositive(beatsPerWave, "Beats Per Wave");

            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float cellWidth = context.Width / count;
            float amplitude = context.Px(values.GetFloat(_amplitude));
            double phase = context.Beats / beatsPerWave * Math.PI * 2;
            double step = values.GetFloat(_phaseStep) * Math.PI / 180;
            for (int index = 0; index < count; index++)
            {
                var clip = set.Wrap(index);
                float lift = amplitude * (float)Math.Sin(phase - index * step);
                float top = context.Height * values.GetFloat(_position) - size / 2 - lift;
                context.Canvas.ImageFit(clip, Frame(clip, context, index * 4), cellWidth * index, top, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
