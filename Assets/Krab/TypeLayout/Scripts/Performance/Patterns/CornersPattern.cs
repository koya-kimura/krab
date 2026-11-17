using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class CornersPattern : TypePattern
    {
        private readonly PrefsString _topLeft;
        private readonly PrefsString _topRight;
        private readonly PrefsString _bottomLeft;
        private readonly PrefsString _bottomRight;
        private readonly PrefsFloat _size;

        public CornersPattern() : base("Corners",
            "Four labels flush to the safe-area corners, by default the event, time, date and tempo.")
        {
            _topLeft = new PrefsString(Key("Top Left"), "{event}");
            _topRight = new PrefsString(Key("Top Right"), "{time}");
            _bottomLeft = new PrefsString(Key("Bottom Left"), "{date}");
            _bottomRight = new PrefsString(Key("Bottom Right"), "{bpm} BPM  {position}");
            _size = new PrefsFloat(Key("Size"), 36);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _topLeft.CreateElement("Top Left"),
                _topRight.CreateElement("Top Right"),
                _bottomLeft.CreateElement("Bottom Left"),
                _bottomRight.CreateElement("Bottom Right"),
                _size.CreateSlider("Size", 8f, 200f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var area = context.SafeArea;
            var top = new LinePath(area.PointAt(Anchor.TopLeft), area.PointAt(Anchor.TopRight));
            var bottom = new LinePath(area.PointAt(Anchor.BottomLeft), area.PointAt(Anchor.BottomRight));
            var size = context.Px(_size.Get());
            var hang = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            var rest = new PlacementOptions(size, vertical: VerticalAnchor.Baseline);
            var start = new FlowOptions(inkFlush: true);
            var end = new FlowOptions(alignment: FlowAlignment.End, inkFlush: true);

            context.Drawer.DrawFlow(top, context.Text(_topLeft.Get()), start, hang, context.Color);
            context.Drawer.DrawFlow(top, context.Text(_topRight.Get()), end, hang, context.Color);
            context.Drawer.DrawFlow(bottom, context.Text(_bottomLeft.Get()), start, rest, context.Color);
            context.Drawer.DrawFlow(bottom, context.Text(_bottomRight.Get()), end, rest, context.Color);
        }
    }
}
