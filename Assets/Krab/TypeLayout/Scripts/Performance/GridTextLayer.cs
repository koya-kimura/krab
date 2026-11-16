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
    /// One upright letter centered in every grid cell. Letters come from the text in order or at random,
    /// and change every few beats (0 keeps them still).
    /// </summary>
    public sealed class GridTextLayer : TextLayer
    {
        private readonly PrefsInt _columns;
        private readonly PrefsInt _rows;
        private readonly PrefsBool _useSafeArea;
        private readonly PrefsBool _random;
        private readonly PrefsInt _seed;
        private readonly PrefsFloat _beatsPerChange;

        private LayoutSlot[] _slots = Array.Empty<LayoutSlot>();
        private (Rect2 Area, int Columns, int Rows) _slotsKey;
        private string _lettersSource;
        private string _letters;

        public GridTextLayer() : base("Grid", false, "Middle Model", 110)
        {
            _columns = new PrefsInt(Key("Columns"), 10);
            _rows = new PrefsInt(Key("Rows"), 6);
            _useSafeArea = new PrefsBool(Key("Use Safe Area"), false);
            _random = new PrefsBool(Key("Random"), true);
            _seed = new PrefsInt(Key("Seed"), 1);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 1);
        }

        protected override Element CreateSettings()
        {
            return UI.Column(
                _columns.CreateSlider("Columns", 1, 40),
                _rows.CreateSlider("Rows", 1, 30),
                _useSafeArea.CreateElement("Use Safe Area"),
                _random.CreateElement("Random"),
                _seed.CreateElement("Seed"),
                _beatsPerChange.CreateSlider("Beats Per Change", 0f, 16f));
        }

        protected override void DrawLayer(in LayerContext context)
        {
            var area = _useSafeArea.Get() ? context.SafeArea : context.Frame.Bounds;
            var key = (area, _columns.Get(), _rows.Get());
            if (!key.Equals(_slotsKey) || _slots.Length == 0)
            {
                _slots = GridDistributor.Distribute(area, key.Item2, key.Item3);
                _slotsKey = key;
            }

            var beatsPerChange = _beatsPerChange.Get();
            var step = beatsPerChange > 0 ? BeatSteps.Step(context.Beats, beatsPerChange) : 0;
            var placement = new PlacementOptions(Size(context), HorizontalAnchor.InkCenter, VerticalAnchor.CapCenter,
                RotationMode.Upright);

            var letters = Letters();
            if (_random.Get())
                context.Drawer.DrawRandom(_slots, letters, _seed.Get(), step, placement, context.Color);
            else
                context.Drawer.DrawSequence(_slots, letters, (int)(step % int.MaxValue), placement, context.Color);
        }
    

        // Spaces would only make empty cells here.
        private string Letters()
        {
            var text = Text;
            if (text == _lettersSource) return _letters;

            _lettersSource = text;
            _letters = text.Replace(" ", string.Empty);
            return _letters;
        }
    }
}
