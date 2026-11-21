using System;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class LineupPattern : ImagePattern
    {
        private const int MaxCount = 12;

        private readonly PrefsInt _count;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _baseline;
        private readonly PrefsBool _stagger;

        public LineupPattern() : base("Lineup",
            "The set's clips side by side, standing on one baseline and playing their frames on the beat.",
            "walk", ImageLook.Original)
        {
            _count = new PrefsInt(Key("Count"), 4);
            _size = new PrefsFloat(Key("Size"), 420);
            _baseline = new PrefsFloat(Key("Baseline"), 0.85f);
            _stagger = new PrefsBool(Key("Stagger"), true);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _count.CreateSlider("Count", 1, MaxCount),
                _size.CreateSlider("Size (height)", 40f, 1080f),
                _baseline.CreateSlider("Baseline (0 top .. 1 bottom)", 0f, 1f),
                _stagger.CreateElement("Stagger Frames"));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float size = context.Px(_size.Get());
            RequirePositive(size, "Size");

            int count = Math.Clamp(_count.Get(), 1, MaxCount);
            float cellWidth = context.Width / count;
            float top = context.Height * _baseline.Get() - size;
            for (int index = 0; index < count; index++)
            {
                var clip = set.Wrap(index);
                double offset = _stagger.Get() ? (double)index * clip.FrameCount / count : 0;
                context.Canvas.ImageFit(clip, Frame(clip, context, offset), cellWidth * index, top, cellWidth, size,
                    FitMode.Contain, 0.5f, 1f);
            }
        }
    }
}
