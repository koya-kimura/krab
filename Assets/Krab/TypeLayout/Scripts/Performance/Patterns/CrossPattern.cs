using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class CrossPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _beatsPerStep;

        public CrossPattern() : base("Cross",
            "A word spread edge to edge across and down through the center, sharing its middle letter; " +
            "the letters shift one cell per step.")
        {
            _text = new PrefsString(Key("Text"), "GIGANDECT");
            _size = new PrefsFloat(Key("Size"), 120);
            _beatsPerStep = new PrefsFloat(Key("Beats Per Step"), 1);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 400f),
                _beatsPerStep.CreateSlider("Beats Per Step", 0.25f, 16f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var text = _text.Get();
            var count = context.Drawer.Clusters(text).Count;
            if (count == 0) return;

            var area = context.SafeArea;
            var step = (int)(BeatSteps.Step(context.Beats, _beatsPerStep.Get()) % count);
            var placement = new PlacementOptions(context.Px(_size.Get()), HorizontalAnchor.InkCenter,
                VerticalAnchor.CapCenter, RotationMode.Upright);
            var across = new LinePath(area.PointAt(Anchor.Left), area.PointAt(Anchor.Right));
            var down = new LinePath(area.PointAt(Anchor.Top), area.PointAt(Anchor.Bottom));
            context.Drawer.DrawSequence(SlotDistributor.Distribute(across, count, SlotSpacing.Cells), text, step,
                placement, context.Color);
            context.Drawer.DrawSequence(SlotDistributor.Distribute(down, count, SlotSpacing.Cells), text, step,
                placement, context.Color);
        }
    }
}
