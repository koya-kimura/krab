using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class FrameLoopPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _speed;

        public FrameLoopPattern() : base("Frame Loop", "Text circling the safe area, hanging inward from its edge.", true)
        {
            _text = new PrefsString(Key("Text"), "Gigandect");
            _size = new PrefsFloat(Key("Size"), 52);
            _speed = new PrefsFloat(Key("Speed"), 40);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _speed.CreateSlider("Speed (px/beat)", -400f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var size = context.Px(_size.Get());
            var path = new RectPerimeterPath(context.SafeArea, cornerGap: size);
            var flow = new FlowOptions(size * 0.25, FlowAlignment.Justify, FlowFill.WholeText,
                BeatSteps.Scroll(context.Beats, context.Px(_speed.Get())));
            var placement = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            context.Drawer.DrawFlow(path, _text.Get(), flow, placement, context.Color);
        }
    }
}
