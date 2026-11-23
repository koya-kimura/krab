using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class LineupPattern : ImagePattern
    {
        private const int MaxCount = 12;

        private readonly PatternParameter _count;
        private readonly PatternParameter _size;
        private readonly PatternParameter _baseline;
        private readonly PatternParameter _stagger;

        public LineupPattern() : base("Lineup",
            "The set's clips side by side, standing on one baseline and playing their frames on the beat.")
        {
            _count = Int("Count", 4, 1, MaxCount);
            _size = Float("Size", 420, 40, 1080, "Size (height)");
            _baseline = Float("Baseline", 0.85, 0, 1, "Baseline (0 top .. 1 bottom)");
            _stagger = Bool("Stagger", true, "Stagger Frames");
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float size = context.Px(values.GetFloat(_size));
            RequirePositive(size, "Size");

            int count = Math.Clamp(values.GetInt(_count), 1, MaxCount);
            float cellWidth = context.Width / count;
            float top = context.Height * values.GetFloat(_baseline) - size;
            for (int index = 0; index < count; index++)
            {
                var clip = set.Wrap(index);
                double offset = values.GetBool(_stagger) ? (double)index * clip.FrameCount / count : 0;
                context.Canvas.ImageFit(clip, Frame(clip, context, offset), cellWidth * index, top, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
