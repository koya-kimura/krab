using System;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// A straight run across the safe area, horizontal or vertical, optionally repeated as a scrolling band.
    /// </summary>
    public sealed class LineTextLayer : TextLayer
    {
        private static readonly string[] AlignmentNames = Enum.GetNames(typeof(FlowAlignment));

        private readonly PrefsBool _vertical;
        private readonly PrefsFloat _position;
        private readonly PrefsInt _alignment;
        private readonly PrefsFloat _tracking;
        private readonly PrefsBool _repeat;
        private readonly PrefsFloat _scrollSpeed;

        public LineTextLayer(string name, bool enabled, string text, bool vertical, float position)
            : base(name, enabled, text, 52)
        {
            _vertical = new PrefsBool(Key("Vertical"), vertical);
            _position = new PrefsFloat(Key("Position"), position);
            _alignment = new PrefsInt(Key("Alignment"), (int)FlowAlignment.Justify);
            _tracking = new PrefsFloat(Key("Tracking"), 12);
            _repeat = new PrefsBool(Key("Repeat"), false);
            _scrollSpeed = new PrefsFloat(Key("Scroll Speed"), 0);
        }

        protected override Element CreateSettings()
        {
            return UI.Column(
                _vertical.CreateElement("Vertical"),
                _position.CreateSlider("Position", 0f, 1f),
                UI.Dropdown("Alignment", () => _alignment.Get(), value => _alignment.Set(value), AlignmentNames),
                _tracking.CreateSlider("Tracking", -20f, 200f),
                _repeat.CreateElement("Repeat"),
                _scrollSpeed.CreateSlider("Scroll (px/beat)", -400f, 400f));
        }

        protected override void DrawLayer(in LayerContext context)
        {
            var area = context.SafeArea;
            var position = Math.Clamp(_position.Get(), 0f, 1f);
            var path = _vertical.Get()
                ? new LinePath(new Point2(area.Left + area.Width * position, area.Top),
                    new Point2(area.Left + area.Width * position, area.Bottom))
                : new LinePath(new Point2(area.Left, area.Top + area.Height * position),
                    new Point2(area.Right, area.Top + area.Height * position));
            var alignment = (FlowAlignment)Math.Clamp(_alignment.Get(), 0, AlignmentNames.Length - 1);
            var offset = BeatSteps.Scroll(context.Beats, context.Frame.ToPixels(_scrollSpeed.Get()));
            var flow = new FlowOptions(context.Frame.ToPixels(_tracking.Get()), alignment,
                _repeat.Get() ? FlowFill.Glyphs : FlowFill.None, offset, inkFlush: true);
            context.Drawer.DrawFlow(path, Text, flow, new PlacementOptions(Size(context)), context.Color);
        }
    }
}
