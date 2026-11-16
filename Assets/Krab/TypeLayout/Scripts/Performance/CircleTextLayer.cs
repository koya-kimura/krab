using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// A ring of repeated text around the output center. Clockwise reads outward, counter-clockwise inward.
    /// </summary>
    public sealed class CircleTextLayer : TextLayer
    {
        private readonly PrefsFloat _radius;
        private readonly PrefsBool _clockwise;
        private readonly PrefsFloat _tracking;
        private readonly PrefsFloat _scrollSpeed;

        public CircleTextLayer() : base("Circle", false, "Middle Model ", 48)
        {
            _radius = new PrefsFloat(Key("Radius"), 400);
            _clockwise = new PrefsBool(Key("Clockwise"), true);
            _tracking = new PrefsFloat(Key("Tracking"), 8);
            _scrollSpeed = new PrefsFloat(Key("Scroll Speed"), 0);
        }

        protected override Element CreateSettings()
        {
            return UI.Column(
                _radius.CreateSlider("Radius", 10f, 1000f),
                _clockwise.CreateElement("Clockwise"),
                _tracking.CreateSlider("Tracking", -20f, 200f),
                _scrollSpeed.CreateSlider("Scroll (px/beat)", -400f, 400f));
        }

        protected override void DrawLayer(in LayerContext context)
        {
            var path = ArcPath.Circle(context.Frame.Bounds.Center, context.Frame.ToPixels(_radius.Get()),
                _clockwise.Get());
            var offset = BeatSteps.Scroll(context.Beats, context.Frame.ToPixels(_scrollSpeed.Get()));
            var flow = new FlowOptions(context.Frame.ToPixels(_tracking.Get()), FlowAlignment.Justify,
                FlowFill.WholeText, offset);
            context.Drawer.DrawFlow(path, Text, flow, new PlacementOptions(Size(context)), context.Color);
        }
    }
}
