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
    public sealed class WavePattern : TypePattern
    {
        private const int Samples = 129;

        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsFloat _amplitude;
        private readonly PrefsFloat _wavelength;
        private readonly PrefsFloat _cyclesPerBeat;
        private readonly Point2[] _points = new Point2[Samples];

        public WavePattern() : base("Wave", "A line of text across the center riding a travelling sine wave.")
        {
            _text = new PrefsString(Key("Text"), "{sub} ");
            _size = new PrefsFloat(Key("Size"), 56);
            _amplitude = new PrefsFloat(Key("Amplitude"), 80);
            _wavelength = new PrefsFloat(Key("Wavelength"), 640);
            _cyclesPerBeat = new PrefsFloat(Key("Cycles Per Beat"), 0.25f);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text"),
                _size.CreateSlider("Size", 8f, 200f),
                _amplitude.CreateSlider("Amplitude", 0f, 400f),
                _wavelength.CreateSlider("Wavelength", 100f, 2000f),
                _cyclesPerBeat.CreateSlider("Cycles Per Beat", -2f, 2f));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var area = context.SafeArea;
            var amplitude = context.Px(_amplitude.Get());
            var wavelength = context.Px(Math.Max(1f, _wavelength.Get()));
            var phase = context.Beats * _cyclesPerBeat.Get();
            var centerY = area.Center.Y;
            for (var i = 0; i < Samples; i++)
            {
                var x = area.Left + area.Width * i / (Samples - 1);
                var y = centerY + amplitude * Math.Sin(2 * Math.PI * ((x - area.Left) / wavelength - phase));
                _points[i] = new Point2(x, y);
            }

            var size = context.Px(_size.Get());
            var flow = new FlowOptions(size * 0.15, fill: FlowFill.Glyphs);
            context.Drawer.DrawFlow(new PolylinePath(_points), context.Text(_text.Get()), flow,
                new PlacementOptions(size), context.Color);
        }
    }
}
