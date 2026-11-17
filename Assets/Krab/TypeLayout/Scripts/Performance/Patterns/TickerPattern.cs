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
    public sealed class TickerPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _rows;
        private readonly PrefsFloat _speed;

        public TickerPattern() : base("Ticker", "Horizontal bands of repeated text scrolling in alternate directions.")
        {
            _text = new PrefsString(Key("Text"), "{sub:upper} ");
            _size = new PrefsFloat(Key("Size"), 72);
            _rows = new PrefsInt(Key("Rows"), 3);
            _speed = new PrefsFloat(Key("Speed"), 60);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 300f),
                _rows.CreateSlider("Rows", 1, 12),
                _speed.CreateSlider("Speed (px/beat)", 0f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var area = context.SafeArea;
            var size = context.Px(_size.Get());
            var scroll = BeatSteps.Scroll(context.Beats, context.Px(_speed.Get()));
            var placement = new PlacementOptions(size);
            var rows = _rows.Get();
            var spine = new LinePath(area.PointAt(Anchor.Top), area.PointAt(Anchor.Bottom));
            var centers = SlotDistributor.Distribute(spine, rows, SlotSpacing.Cells);
            for (var row = 0; row < rows; row++)
            {
                var y = centers[row].Position.Y;
                var line = new LinePath(new Point2(area.Left, y), new Point2(area.Right, y));
                var offset = row % 2 == 0 ? scroll : -scroll;
                var flow = new FlowOptions(size * 0.1, fill: FlowFill.Glyphs, offset: offset);
                context.Drawer.DrawFlow(line, context.Text(_text.Get()), flow, placement, context.Color);
            }
        }
    }
}
