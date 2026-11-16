using System;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class TunnelPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _rings;
        private readonly PrefsFloat _gap;
        private readonly PrefsFloat _speed;

        public TunnelPattern() : base("Tunnel",
            "Nested rectangles of text inside the safe area, each ring running opposite to its neighbours.")
        {
            _text = new PrefsString(Key("Text"), "Gigandect");
            _size = new PrefsFloat(Key("Size"), 36);
            _rings = new PrefsInt(Key("Rings"), 6);
            _gap = new PrefsFloat(Key("Gap"), 70);
            _speed = new PrefsFloat(Key("Speed"), 40);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _rings.CreateSlider("Rings", 1, 20),
                _gap.CreateSlider("Gap", 10f, 300f),
                _speed.CreateSlider("Speed (px/beat)", -400f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var size = context.Px(_size.Get());
            var gap = context.Px(_gap.Get());
            var scroll = BeatSteps.Scroll(context.Beats, context.Px(_speed.Get()));
            var placement = new PlacementOptions(size, vertical: VerticalAnchor.CapTop);
            for (var ring = 0; ring < Math.Max(1, _rings.Get()); ring++)
            {
                // Stop once a ring would be too small to hold text on its short sides.
                var inset = gap * ring;
                var shortSide = Math.Min(context.SafeArea.Width, context.SafeArea.Height) - inset * 2;
                if (shortSide <= size * 4) break;

                var path = new RectPerimeterPath(context.SafeArea.Inset(inset), cornerGap: size);
                var flow = new FlowOptions(size * 0.25, FlowAlignment.Justify, FlowFill.WholeText,
                    ring % 2 == 0 ? scroll : -scroll);
                context.Drawer.DrawFlow(path, _text.Get(), flow, placement, context.Color);
            }
        }
    }
}
