using System;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class DialPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _radius;
        private readonly PrefsFloat _beatsPerStep;

        public DialPattern() : base("Dial",
            "Upright letters evenly spaced on a circle like a clock face, the whole ring ticking one place per step.")
        {
            _text = new PrefsString(Key("Text"), "0123456789AB");
            _size = new PrefsFloat(Key("Size"), 56);
            _radius = new PrefsFloat(Key("Radius"), 300);
            _beatsPerStep = new PrefsFloat(Key("Beats Per Step"), 1);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 300f),
                _radius.CreateSlider("Radius", 20f, 1000f),
                _beatsPerStep.CreateSlider("Beats Per Step", 0.25f, 16f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var letters = Letters(_text.Get());
            var count = context.Drawer.Clusters(letters).Count;
            if (count == 0) return;

            // Moving the start angle by one slot per step keeps every letter on an exact twelfth (or nth).
            var step = BeatSteps.Step(context.Beats, Math.Max(0.25f, _beatsPerStep.Get())) % count;
            var circle = new ArcPath(context.Frame.Bounds.Center, context.Px(_radius.Get()), -90 + 360.0 * step / count,
                360);
            var slots = SlotDistributor.Distribute(circle, count, SlotSpacing.Ends);
            var placement = new PlacementOptions(context.Px(_size.Get()), HorizontalAnchor.InkCenter,
                VerticalAnchor.InkCenter, RotationMode.Upright);
            context.Drawer.DrawSequence(slots, letters, 0, placement, context.Color);
        }
    }
}
