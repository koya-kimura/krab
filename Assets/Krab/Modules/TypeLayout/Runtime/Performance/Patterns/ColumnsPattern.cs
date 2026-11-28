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
    public sealed class ColumnsPattern : TypePattern
    {
        // Relative speeds per column, repeating, so neighbouring columns drift apart.
        private static readonly double[] SpeedRatios = { 1, 1.6, 0.7, 1.3, 0.9, 1.9 };

        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _columns;
        private readonly PrefsFloat _speed;

        public ColumnsPattern() : base("Columns", "Vertical runs of text falling at different speeds.")
        {
            _text = new PrefsString(Key("Text"), "{main}");
            _size = new PrefsFloat(Key("Size"), 48);
            _columns = new PrefsInt(Key("Columns"), 6);
            _speed = new PrefsFloat(Key("Speed"), 40);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _columns.CreateSlider("Columns", 1, 24),
                _speed.CreateSlider("Speed (px/beat)", 0f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var area = context.SafeArea;
            var size = context.Px(_size.Get());
            var columns = _columns.Get();
            var placement = new PlacementOptions(size);
            var axis = new LinePath(area.PointAt(Anchor.Left), area.PointAt(Anchor.Right));
            var centers = SlotDistributor.Distribute(axis, columns, SlotSpacing.Cells);
            for (var column = 0; column < columns; column++)
            {
                var x = centers[column].Position.X;
                var line = new LinePath(new Point2(x, area.Top), new Point2(x, area.Bottom));
                var speed = context.Px(_speed.Get()) * SpeedRatios[column % SpeedRatios.Length];
                var flow = new FlowOptions(size * 0.15, fill: FlowFill.Glyphs,
                    offset: BeatSteps.Scroll(context.Beats, speed));
                context.Drawer.DrawFlow(line, context.Text(_text.Get()), flow, placement, context.Color);
            }
        }
    }
}
