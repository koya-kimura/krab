using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class CenterPattern : ImagePattern
    {
        private readonly PatternParameter _size;
        private readonly PatternParameter _beatsPerChange;
        private readonly PatternParameter _swing;
        private readonly PatternParameter _beatsPerSwing;

        public CenterPattern() : base("Center",
            "One clip large in the middle, standing on its bottom edge and swinging from there. Steps to the " +
            "next clip of the set on the beat.")
        {
            _size = Float("Size", 0.85, 0.1, 1.5, "Size (screen height)");
            _beatsPerChange = Float("Beats Per Change", 4, 0.25, 32);
            _swing = Float("Swing", 3, 0, 45, "Swing (± degrees)");
            _beatsPerSwing = Float("Beats Per Swing", 2, 0.25, 16);
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set, ParameterValues values)
        {
            float boxHeight = context.Height * values.GetFloat(_size);
            RequirePositive(boxHeight, "Size");
            float beatsPerChange = values.GetFloat(_beatsPerChange);
            RequirePositive(beatsPerChange, "Beats Per Change");
            float beatsPerSwing = values.GetFloat(_beatsPerSwing);
            RequirePositive(beatsPerSwing, "Beats Per Swing");

            var clip = set.Wrap((int)(BeatSteps.Step(context.Beats, beatsPerChange) % int.MaxValue));
            float boxWidth = context.Width * 0.9f;
            float angle = (Zigzag(context.Beats, beatsPerSwing) * 2 - 1) * values.GetFloat(_swing);
            var canvas = context.Canvas;
            canvas.Translate(context.Width / 2, (context.Height + boxHeight) / 2);
            canvas.Rotate(angle);
            canvas.ImageFit(clip, Frame(clip, context), -boxWidth / 2, -boxHeight, boxWidth, boxHeight,
                FitMode.Contain, 0.5f, 1f);
        }
    }
}
