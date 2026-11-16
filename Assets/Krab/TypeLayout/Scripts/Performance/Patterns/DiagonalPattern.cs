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
    public sealed class DiagonalPattern : TypePattern
    {
        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _lines;
        private readonly PrefsFloat _angle;
        private readonly PrefsFloat _speed;

        public DiagonalPattern() : base("Diagonal",
            "Parallel slanted bands of text crossing the whole frame, scrolling in alternate directions.")
        {
            _text = new PrefsString(Key("Text"), "GIGANDECT ");
            _size = new PrefsFloat(Key("Size"), 48);
            _lines = new PrefsInt(Key("Lines"), 7);
            _angle = new PrefsFloat(Key("Angle"), -30);
            _speed = new PrefsFloat(Key("Speed"), 50);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _lines.CreateSlider("Lines", 1, 24),
                _angle.CreateSlider("Angle", -90f, 90f),
                _speed.CreateSlider("Speed (px/beat)", 0f, 400f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var bounds = context.Frame.Bounds;
            var center = bounds.Center;
            var direction = Point2.FromAngle(_angle.Get());
            var normal = direction.Rotate(90);
            var reach = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
            var size = context.Px(_size.Get());
            var scroll = BeatSteps.Scroll(context.Beats, context.Px(_speed.Get()));
            var lines = Math.Max(1, _lines.Get());
            for (var line = 0; line < lines; line++)
            {
                // Lines are evenly spaced across the frame's extent along the normal, then clipped to the frame.
                var offset = -reach + 2 * reach * (2 * line + 1) / (2 * lines);
                var origin = center + normal * offset;
                if (!Clip(bounds, origin, direction, out var start, out var end) || end - start < size) continue;

                var path = new LinePath(origin + direction * start, origin + direction * end);
                var flow = new FlowOptions(size * 0.15, fill: FlowFill.Glyphs,
                    offset: line % 2 == 0 ? scroll : -scroll);
                context.Drawer.DrawFlow(path, _text.Get(), flow, new PlacementOptions(size), context.Color);
            }
        }

        // Liang–Barsky: the parameter range of origin + direction * t that lies inside the rectangle.
        private static bool Clip(Rect2 rect, Point2 origin, Point2 direction, out double start, out double end)
        {
            start = double.NegativeInfinity;
            end = double.PositiveInfinity;
            return ClipAxis(origin.X, direction.X, rect.Left, rect.Right, ref start, ref end)
                   && ClipAxis(origin.Y, direction.Y, rect.Top, rect.Bottom, ref start, ref end);
        }

        private static bool ClipAxis(double origin, double direction, double min, double max, ref double start,
            ref double end)
        {
            if (Math.Abs(direction) < 1e-12) return origin >= min && origin <= max;

            var a = (min - origin) / direction;
            var b = (max - origin) / direction;
            start = Math.Max(start, Math.Min(a, b));
            end = Math.Min(end, Math.Max(a, b));
            return start < end;
        }
    }
}
