using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>Text repeated around the safe area, hanging inward from its edge, closing seamlessly.</summary>
    public sealed class FrameTextLayer : TextLayer
    {
        private readonly PrefsFloat _inset;
        private readonly PrefsFloat _tracking;
        private readonly PrefsBool _cornerGap;
        private readonly PrefsFloat _scrollSpeed;

        public FrameTextLayer() : base("Frame", true, "Gigandect", 52)
        {
            _inset = new PrefsFloat(Key("Inset"), 0);
            _tracking = new PrefsFloat(Key("Tracking"), 12);
            _cornerGap = new PrefsBool(Key("Corner Gap"), true);
            _scrollSpeed = new PrefsFloat(Key("Scroll Speed"), 0);
        }

        protected override Element CreateSettings()
        {
            return UI.Column(
                _inset.CreateSlider("Inset", 0f, 400f),
                _tracking.CreateSlider("Tracking", -20f, 200f),
                _cornerGap.CreateElement("Corner Gap"),
                _scrollSpeed.CreateSlider("Scroll (px/beat)", -400f, 400f));
        }

        protected override void DrawLayer(in LayerContext context)
        {
            var size = Size(context);
            var rect = context.SafeArea.Inset(context.Frame.ToPixels(_inset.Get()));
            var path = new RectPerimeterPath(rect, cornerGap: _cornerGap.Get() ? size : 0);
            var offset = BeatSteps.Scroll(context.Beats, context.Frame.ToPixels(_scrollSpeed.Get()));
            var flow = new FlowOptions(context.Frame.ToPixels(_tracking.Get()), FlowAlignment.Justify,
                FlowFill.WholeText, offset);
            var placement = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            context.Drawer.DrawFlow(path, Text, flow, placement, context.Color);
        }
    }
}
