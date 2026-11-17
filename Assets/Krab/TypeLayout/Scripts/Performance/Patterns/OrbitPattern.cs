using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class OrbitPattern : TypePattern
    {
        private readonly PrefsString _outerText;
        private readonly PrefsString _innerText;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _radius;
        private readonly PrefsFloat _speed;

        public OrbitPattern() : base("Orbit",
            "Two rings around the center turning in opposite directions; the outer reads outward, the inner inward.")
        {
            _outerText = new PrefsString(Key("Outer Text"), "{sub} ");
            _innerText = new PrefsString(Key("Inner Text"), "{main} ");
            _size = new PrefsFloat(Key("Size"), 44);
            _radius = new PrefsFloat(Key("Radius"), 420);
            _speed = new PrefsFloat(Key("Speed"), 30);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _outerText.CreateElement("Outer Text"),
                _innerText.CreateElement("Inner Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _radius.CreateSlider("Radius", 50f, 1000f),
                _speed.CreateSlider("Speed (px/beat)", -400f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var center = context.Frame.Bounds.Center;
            var size = context.Px(_size.Get());
            var radius = context.Px(_radius.Get());
            var scroll = BeatSteps.Scroll(context.Beats, context.Px(_speed.Get()));
            var placement = new PlacementOptions(size);

            // Both rings move forward along their own direction, so they turn opposite ways on screen.
            var ring = new FlowOptions(size * 0.15, FlowAlignment.Justify, FlowFill.WholeText, scroll);
            context.Drawer.DrawFlow(ArcPath.Circle(center, radius), context.Text(_outerText.Get()), ring, placement,
                context.Color);
            context.Drawer.DrawFlow(ArcPath.Circle(center, radius - size * 1.8, clockwise: false),
                context.Text(_innerText.Get()), ring, placement, context.Color);
        }
    }
}
