using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Motion;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Krab.TypeLayout.Core.Text;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;

namespace Krab.TypeLayout.Performance.Patterns
{
    public sealed class ScatterPattern : TypePattern
    {
        // Words land in distinct cells of this grid, so they never overlap.
        private const int GridColumns = 4;
        private const int GridRows = 8;
        private const int CellCount = GridColumns * GridRows;

        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _count;
        private readonly PrefsFloat _beatsPerChange;
        private readonly PrefsInt _seed;
        private readonly List<LayoutSlot> _slots = new();
        private readonly List<string> _words = new();
        private readonly int[] _cells = new int[CellCount];

        private string _wordsSource;
        private string[] _wordList = Array.Empty<string>();

        public ScatterPattern() : base("Scatter",
            "Entries of the text (split by |) dropped into seeded random cells, upright or upside down, " +
            "re-rolled on the beat.")
        {
            _text = new PrefsString(Key("Text"), "{list:upper}");
            _size = new PrefsFloat(Key("Size"), 56);
            _count = new PrefsInt(Key("Count"), 14);
            _beatsPerChange = new PrefsFloat(Key("Beats Per Change"), 2);
            _seed = new PrefsInt(Key("Seed"), 3);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text (entries split by |)"),
                _size.CreateSlider("Size", 8f, 300f),
                _count.CreateSlider("Count", 1, CellCount),
                _beatsPerChange.CreateSlider("Beats Per Change", 0.25f, 16f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var words = Words(context.Text(_text.Get()));
            if (words.Length == 0) return;

            var size = context.Px(_size.Get());
            var seed = _seed.Get();
            var step = BeatSteps.Step(context.Beats, Math.Max(0.25f, _beatsPerChange.Get()));
            var count = Math.Clamp(_count.Get(), 0, CellCount);

            // Partial Fisher–Yates over the cells, driven by the seeded hash so each step is reproducible.
            for (var i = 0; i < CellCount; i++)
                _cells[i] = i;

            _slots.Clear();
            _words.Clear();
            for (var i = 0; i < count; i++)
            {
                var pick = i + (int)(TextPicker.Hash(seed, i * 3, step) % (ulong)(CellCount - i));
                (_cells[i], _cells[pick]) = (_cells[pick], _cells[i]);
                var cell = GridDistributor.Cell(context.SafeArea, GridColumns, GridRows, _cells[i] % GridColumns,
                    _cells[i] / GridColumns);
                var turn = TextPicker.Hash(seed, i * 3 + 1, step) % 2 == 0 ? 0 : 180;
                _slots.Add(new LayoutSlot(cell.Center, turn));
                _words.Add(words[(int)(TextPicker.Hash(seed, i * 3 + 2, step) % (ulong)words.Length)]);
            }

            var placement = new PlacementOptions(size, HorizontalAnchor.InkCenter, VerticalAnchor.CapCenter);
            context.Drawer.DrawTexts(_slots, _words, placement, context.Color);
        }

        private string[] Words(string text)
        {
            if (text == _wordsSource) return _wordList;

            _wordsSource = text;
            var entries = new List<string>();
            foreach (var entry in (text ?? string.Empty).Split('|'))
            {
                var trimmed = entry.Trim();
                if (trimmed.Length > 0) entries.Add(trimmed);
            }

            _wordList = entries.ToArray();
            return _wordList;
        }
    }
}
