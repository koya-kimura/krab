using System;
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
    public sealed class ZigzagPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _teeth;
        private readonly PrefsFloat _height;
        private readonly PrefsFloat _speed;

        private Point2[] _points = Array.Empty<Point2>();

        public ZigzagPattern() : base("Zigzag",
            "Text running along a zigzag across the center, turning sharply at each tooth.")
        {
            _text = new PrefsString(Key("Text"), "Middle Model ");
            _size = new PrefsFloat(Key("Size"), 44);
            _teeth = new PrefsInt(Key("Teeth"), 4);
            _height = new PrefsFloat(Key("Height"), 360);
            _speed = new PrefsFloat(Key("Speed"), 60);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _teeth.CreateSlider("Teeth", 1, 16),
                _height.CreateSlider("Height", 0f, 1000f),
                _speed.CreateSlider("Speed (px/beat)", -400f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var area = context.SafeArea;
            var corners = Math.Max(1, _teeth.Get()) * 2 + 1;
            if (_points.Length != corners) _points = new Point2[corners];

            var half = context.Px(_height.Get()) / 2;
            for (var i = 0; i < corners; i++)
            {
                var x = area.Left + area.Width * i / (corners - 1);
                _points[i] = new Point2(x, area.Center.Y + (i % 2 == 0 ? half : -half));
            }

            var size = context.Px(_size.Get());
            var flow = new FlowOptions(size * 0.12, fill: FlowFill.Glyphs,
                offset: BeatSteps.Scroll(context.Beats, context.Px(_speed.Get())));
            context.Drawer.DrawFlow(new PolylinePath(_points), _text.Get(), flow, new PlacementOptions(size),
                context.Color);
        }
    }
}
