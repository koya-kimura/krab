using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using Krab.TypeLayout.Core.Motion;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.ImageLayer.Performance.Patterns
{
    public sealed class CenterPattern : ImagePattern
    {
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsFloat _swing;
        private readonly PrefsFloat _beatsPerSwing;

        public CenterPattern() : base("Center",
            "One clip large in the middle, standing on its bottom edge and swinging from there. Steps to the " +
            "next clip of the set on the beat.",
            "human", ImageLook.Original)
        {
            _size = new PrefsFloat(Key("Size"), 0.85f);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 4);
            _swing = new PrefsFloat(Key("Swing"), 3);
            _beatsPerSwing = new PrefsFloat(Key("Beats Per Swing"), 2);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _size.CreateSlider("Size (screen height)", 0.1f, 1.5f),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.25f, 32f),
                _swing.CreateSlider("Swing (± degrees)", 0f, 45f),
                _beatsPerSwing.CreateSlider("Beats Per Swing", 0.25f, 16f));
        }

        protected override void DrawPattern(in ImagePatternContext context, ImageSet set)
        {
            float boxHeight = context.Height * _size.Get();
            RequirePositive(boxHeight, "Size");
            float beatsPerChange = _beatsPerChange.Get();
            RequirePositive(beatsPerChange, "Beats Per Change");
            float beatsPerSwing = _beatsPerSwing.Get();
            RequirePositive(beatsPerSwing, "Beats Per Swing");

            var clip = set.Wrap((int)(BeatSteps.Step(context.Beats, beatsPerChange) % int.MaxValue));
            float boxWidth = context.Width * 0.9f;
            float angle = (Zigzag(context.Beats, beatsPerSwing) * 2 - 1) * _swing.Get();
            var canvas = context.Canvas;
            canvas.Translate(context.Width / 2, (context.Height + boxHeight) / 2);
            canvas.Rotate(angle);
            canvas.ImageFit(clip, Frame(clip, context), -boxWidth / 2, -boxHeight, boxWidth, boxHeight,
                FitMode.Contain, 0.5f, 1f);
        }
    }
}
