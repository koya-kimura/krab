using System;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class RadialPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _spokes;
        private readonly PrefsFloat _innerRadius;
        private readonly PrefsFloat _degreesPerBeat;

        public RadialPattern() : base("Radial", "Spokes of text reading outward from the center, slowly turning.")
        {
            _text = new PrefsString(Key("Text"), "{main:upper}");
            _size = new PrefsFloat(Key("Size"), 40);
            _spokes = new PrefsInt(Key("Spokes"), 12);
            _innerRadius = new PrefsFloat(Key("Inner Radius"), 120);
            _degreesPerBeat = new PrefsFloat(Key("Degrees Per Beat"), 3);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _spokes.CreateSlider("Spokes", 1, 48),
                _innerRadius.CreateSlider("Inner Radius", 0f, 500f),
                _degreesPerBeat.CreateSlider("Degrees Per Beat", -45f, 45f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var bounds = context.Frame.Bounds;
            var center = bounds.Center;
            var inner = context.Px(_innerRadius.Get());
            var outer = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
            if (outer <= inner) return;

            var size = context.Px(_size.Get());
            var spokes = Math.Max(1, _spokes.Get());
            var turn = Angles.Normalize(context.Beats * _degreesPerBeat.Get());
            var flow = new FlowOptions(size * 0.15, fill: FlowFill.Glyphs, keepOverflow: true);
            for (var spoke = 0; spoke < spokes; spoke++)
            {
                var direction = Point2.FromAngle(turn + 360.0 * spoke / spokes);
                var path = new LinePath(center + direction * inner, center + direction * outer);
                context.Drawer.DrawFlow(path, context.Text(_text.Get()), flow, new PlacementOptions(size), context.Color);
            }
        }
    }
}
