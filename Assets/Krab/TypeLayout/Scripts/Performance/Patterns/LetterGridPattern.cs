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
    public sealed class LetterGridPattern : TypePattern
    {
        private readonly PrefsString _letters;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _rows;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsInt _seed;

        private LayoutSlot[] _slots = Array.Empty<LayoutSlot>();
        private (Rect2 Area, int Columns, int Rows) _slotsKey;

        public LetterGridPattern() : base("Letter Grid",
            "One upright letter centered in every cell, picked at random from the letters and re-rolled on the beat.")
        {
            _letters = new PrefsString(Key("Letters"), "Model");
            _size = new PrefsFloat(Key("Size"), 110);
            _rows = new PrefsInt(Key("Rows"), 6);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 1);
            _seed = new PrefsInt(Key("Seed"), 1);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _letters.CreateElement("Letters"),
                _size.CreateSlider("Size", 8f, 400f),
                _rows.CreateSlider("Rows", 1, 30),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.25f, 16f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            // Square-ish cells: the column count follows the output aspect ratio.
            var area = context.Frame.Bounds;
            var rows = Math.Max(1, _rows.Get());
            var columns = Math.Max(1, (int)Math.Floor(rows * area.Width / area.Height));
            var key = (area, columns, rows);
            if (!key.Equals(_slotsKey) || _slots.Length == 0)
            {
                _slots = GridDistributor.Distribute(area, columns, rows);
                _slotsKey = key;
            }

            var step = BeatSteps.Step(context.Beats, Math.Max(0.25f, _beatsPerChange.Get()));
            var placement = new PlacementOptions(context.Px(_size.Get()), HorizontalAnchor.InkCenter,
                VerticalAnchor.CapCenter, RotationMode.Upright);
            context.Drawer.DrawRandom(_slots, _letters.Get().Replace(" ", string.Empty), _seed.Get(), step,
                placement, context.Color);
        }
    }
}
