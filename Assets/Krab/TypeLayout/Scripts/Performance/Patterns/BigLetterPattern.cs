using System;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class BigLetterPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _beatsPerStep;
        private readonly LayoutSlot[] _slot = new LayoutSlot[1];

        public BigLetterPattern() : base("Big Letter",
            "One huge letter at the center whose shape is centered exactly, stepping through the text.")
        {
            _text = new PrefsString(Key("Text"), "KRAB");
            _size = new PrefsFloat(Key("Size"), 760);
            _beatsPerStep = new PrefsFloat(Key("Beats Per Step"), 1);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 50f, 1400f),
                _beatsPerStep.CreateSlider("Beats Per Step", 0.25f, 16f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            _slot[0] = new LayoutSlot(context.Frame.Bounds.Center, 0);
            var step = BeatSteps.Step(context.Beats, Math.Max(0.25f, _beatsPerStep.Get()));
            var placement = new PlacementOptions(context.Px(_size.Get()), HorizontalAnchor.InkCenter,
                VerticalAnchor.InkCenter, RotationMode.Upright);
            context.Drawer.DrawSequence(_slot, Letters(_text.Get()), (int)(step % int.MaxValue), placement,
                context.Color);
        }
    }
}
