using System;
using System.Collections.Generic;
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
    public sealed class SpiralPattern : TypePattern
    {
        private const int SamplesPerTurn = 72;

        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _turns;
        private readonly PrefsFloat _speed;
        private readonly List<Point2> _points = new();

        public SpiralPattern() : base("Spiral", "Text unwinding from the center along a spiral, flowing outward.")
        {
            _text = new PrefsString(Key("Text"), "Middle Model ");
            _size = new PrefsFloat(Key("Size"), 40);
            _turns = new PrefsFloat(Key("Turns"), 4);
            _speed = new PrefsFloat(Key("Speed"), 60);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _turns.CreateSlider("Turns", 1f, 12f),
                _speed.CreateSlider("Speed (px/beat)", -400f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            // Archimedean spiral, clockwise on screen so glyph tops face outward.
            var center = context.Frame.Bounds.Center;
            var size = context.Px(_size.Get());
            var turns = Math.Max(0.5, _turns.Get());
            var inner = size;
            var outer = context.SafeArea.Height / 2;
            var samples = (int)Math.Ceiling(turns * SamplesPerTurn);
            _points.Clear();
            for (var i = 0; i <= samples; i++)
            {
                var t = (double)i / samples;
                var radius = inner + (outer - inner) * t;
                _points.Add(center + Point2.FromAngle(-90 + 360 * turns * t) * radius);
            }

            var flow = new FlowOptions(size * 0.12, fill: FlowFill.Glyphs,
                offset: BeatSteps.Scroll(context.Beats, context.Px(_speed.Get())));
            context.Drawer.DrawFlow(new PolylinePath(_points), _text.Get(), flow, new PlacementOptions(size),
                context.Color);
        }
    }
}
