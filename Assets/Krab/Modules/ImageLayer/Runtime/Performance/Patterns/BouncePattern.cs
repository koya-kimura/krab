using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class BouncePattern : ImagePattern
    {
        private const int MaxCount = 16;
        private const float Squash = 0.2f;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _jump;
        private readonly PatternParameter _beatsPerBounce;
        private readonly PatternParameter _stagger;
        private readonly PatternParameter _floor;

        public BouncePattern() : base("Bounce",
            "A row of clips hopping on the beat, squashing as they land. Stagger spreads the hops along the row.")
        {
            _count = Int("Count", 6, 1, MaxCount);
            _size = Float("Size", 320, 40, 800);
            _jump = Float("Jump", 260, 0, 800);
            _beatsPerBounce = Float("Beats Per Bounce", 1, 0.25, 8);
            _stagger = Float("Stagger", 0.25, 0, 1, "Stagger (bounces between neighbors)");
            _floor = Float("Floor", 0.9, 0, 1, "Floor (0 top .. 1 bottom)");
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");
            float beatsPerBounce = values.GetFloat(_beatsPerBounce);
            RequirePositive(beatsPerBounce, "Beats Per Bounce");

            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float cellWidth = context.Width / count;
            float floor = context.Height * values.GetFloat(_floor);
            float jump = context.Px(values.GetFloat(_jump));
            var canvas = context.Canvas;
            for (int index = 0; index < count; index++)
            {
                double position = context.Beats / beatsPerBounce - index * values.GetFloat(_stagger);
                float phase = (float)(position - Math.Floor(position));

                // Parabola: on the floor at the beat, highest halfway; squashed for a moment around landing.
                float height = 1f - (2f * phase - 1f) * (2f * phase - 1f);
                float squash = Squash * Math.Max(0f, 1f - height * 4f);
                var clip = set.Wrap(index);

                canvas.Push();
                canvas.Translate(cellWidth * (index + 0.5f), floor - jump * height);
                canvas.Scale(1f + squash, 1f - squash);
                canvas.ImageFit(clip, Frame(clip, context, index * 4), -cellWidth / 2, -size, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
                canvas.Pop();
            }
        }
    }
}
