using System;
using System.Collections.Generic;
using Krab.Common.Utils;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Krab.TypeLayout.Unity.Rendering;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.TypeLayout.Performance.Patterns
{
    /// <summary>
    /// Copies of one text block scattered over the screen. Each copy types in, holds and erases from the front on
    /// its own offset of the cycle, and its words flicker behind colored highlights.
    /// </summary>
    public sealed class FlickerPattern : TypePattern
    {
        private const int MaxBlocks = 40;
        private const double LineHeightEm = 1.2;
        private const double HighlightPadEm = 0.12;
        private const double MarkerGapEm = 0.4;
        private const double BlockGapEm = 0.6;
        private const uint MarkerSalt = 0x7F4Au;

        private static readonly EasingFn Linear = x => x;

        // Five gray steps: text on light highlights is black, on dark ones white.
        private static readonly Highlight[] Palette =
        {
            new(new Color(1f, 1f, 1f), true),
            new(new Color(0.8f, 0.8f, 0.8f), true),
            new(new Color(0.6f, 0.6f, 0.6f), true),
            new(new Color(0.4f, 0.4f, 0.4f), false),
            new(new Color(0.2f, 0.2f, 0.2f), false)
        };

        private readonly PrefsString _text;
        private readonly PrefsFloat _size;
        private readonly PrefsInt _blocks;
        private readonly PrefsFloat _beatsPerCycle;
        private readonly PrefsFloat _hold;
        private readonly PrefsFloat _flickerBeats;
        private readonly PrefsFloat _highlightChance;
        private readonly PrefsInt _seed;
        private readonly FlickerText _layout = new();
        private readonly FlickerPlacer _placer = new(MaxBlocks);
        private readonly List<(Rect2 Rect, Color Color)> _fills = new();
        private readonly TextBatch _plain = new();
        private readonly TextBatch _onLight = new();
        private readonly TextBatch _onDark = new();

        public FlickerPattern() : base("Flicker",
            "Copies of a text block (lines split by |) that type in and erase on staggered offsets, with words " +
            "flickering behind colored highlights.")
        {
            _text = new PrefsString(Key("Text"), "{main:upper} * {sub:upper}|{bpm} BPM * {time}|{date}");
            _size = new PrefsFloat(Key("Size"), 28);
            _blocks = new PrefsInt(Key("Blocks"), 18);
            _beatsPerCycle = new PrefsFloat(Key("Beats Per Cycle"), 8);
            _hold = new PrefsFloat(Key("Hold"), 0.3f);
            _flickerBeats = new PrefsFloat(Key("Flicker Beats"), 0.125f);
            _highlightChance = new PrefsFloat(Key("Highlight Chance"), 0.35f);
            _seed = new PrefsInt(Key("Seed"), 5);
        }

        protected override Element CreateParameters()
        {
            return UI.Column(
                _text.CreateElement("Text (lines split by |)"),
                _size.CreateSlider("Size", 8f, 120f),
                _blocks.CreateSlider("Blocks", 1, MaxBlocks),
                _beatsPerCycle.CreateSlider("Beats Per Cycle", 1f, 32f),
                _hold.CreateSlider("Hold", 0f, 0.9f),
                _flickerBeats.CreateSlider("Flicker Beats", 0.0625f, 2f),
                _highlightChance.CreateSlider("Highlight Chance", 0f, 1f),
                _seed.CreateElement("Seed"));
        }

        protected override void DrawPattern(in PatternContext context)
        {
            var size = context.Px(_size.Get());
            var placement = new PlacementOptions(size, HorizontalAnchor.AdvanceCenter, VerticalAnchor.Baseline,
                snapToPixel: true);
            _layout.Update(context.Text(_text.Get()), size, context.Drawer);
            if (_layout.Glyphs.Count == 0) return;

            _fills.Clear();
            _plain.Clear();
            _onLight.Clear();
            _onDark.Clear();

            var seed = unchecked((uint)_seed.Get());
            var beats = (float)context.Beats;
            var cycle = Mathf.Max(0.25f, _beatsPerCycle.Get());
            var flickerStep = (uint)Rhythm.GetIndex(beats, Mathf.Max(0.03125f, _flickerBeats.Get()));
            var blocks = Math.Clamp(_blocks.Get(), 0, MaxBlocks);
            var lineHeight = size * LineHeightEm;

            // The trailing marker and highlight padding stick out past the text, so reserve room for them.
            var blockWidth = _layout.Width + size * (MarkerGapEm + 1);
            _placer.Prepare(context.SafeArea, blockWidth, _layout.LineCount * lineHeight, size, lineHeight,
                size * BlockGapEm, seed, blocks);
            for (var block = 0; block < blocks; block++)
            {
                // Each block runs the same cycle shifted by its own seeded offset, so the copies are always at
                // different stages of typing in and erasing.
                var offset = Pcg.Pcg01(new UInt2(seed, (uint)block)).x * cycle;
                var phase = Rhythm.GetPhase(beats + offset, cycle, cycle, Linear);
                var cycleIndex = Mathf.FloorToInt(phase);
                if (!_placer.TryGetOrigin(block, cycleIndex, out var origin)) continue;

                AddBlock(context, seed, (uint)block, origin, phase - cycleIndex, flickerStep, size);
            }

            foreach (var (rect, color) in _fills)
                context.Drawer.FillRect(rect, color);

            var plainGray = context.Color.grayscale;
            _plain.Draw(context.Drawer, placement, new Color(plainGray, plainGray, plainGray, context.Color.a));
            _onLight.Draw(context.Drawer, placement, Color.black);
            _onDark.Draw(context.Drawer, placement, Color.white);
        }

        private void AddBlock(in PatternContext context, uint seed, uint block, Point2 origin, float progress,
            uint flickerStep, double size)
        {
            var glyphs = _layout.Glyphs;
            var (tail, head) = VisibleRange(progress, Mathf.Clamp(_hold.Get(), 0f, 0.9f), glyphs.Count);
            if (head <= tail) return;

            var lineHeight = size * LineHeightEm;
            var capHeight = context.Drawer.Metrics.CapHeight * size;
            var pad = size * HighlightPadEm;
            var chance = Mathf.Clamp01(_highlightChance.Get());

            // Consecutive visible clusters of one word share a highlight rectangle.
            var runWord = -1;
            var runLeft = 0.0;
            var runRight = 0.0;
            var runBaseline = 0.0;
            var runHighlight = -1;
            for (var i = tail; i < head; i++)
            {
                var glyph = glyphs[i];
                var baseline = origin.Y + glyph.Line * lineHeight + (lineHeight + capHeight) / 2;
                if (glyph.Word != runWord || baseline != runBaseline)
                {
                    AddFill(runHighlight, origin.X, runLeft, runRight, runBaseline, capHeight, pad);
                    runWord = glyph.Word;
                    runLeft = glyph.X;
                    runBaseline = baseline;
                    runHighlight = glyph.IsBlank
                        ? -1
                        : HighlightIndex(seed, block, (uint)glyph.Word, flickerStep, chance);
                }

                runRight = glyph.X + glyph.Advance;
                if (glyph.IsBlank) continue;

                var slot = new LayoutSlot(new Point2(origin.X + glyph.X + glyph.Advance / 2, baseline), 0);
                var batch = runHighlight < 0 ? _plain : Palette[runHighlight].DarkText ? _onLight : _onDark;
                batch.Add(slot, glyph.Text);
            }

            AddFill(runHighlight, origin.X, runLeft, runRight, runBaseline, capHeight, pad);

            // A small square follows the cursor (the last visible cluster).
            var markerSide = capHeight * 0.8;
            var markerLeft = origin.X + runRight + size * MarkerGapEm;
            var markerTop = runBaseline - (capHeight + markerSide) / 2;
            var markerColor = Palette[PaletteIndex(Pcg.Pcg01(new UInt4(seed, block, MarkerSalt, flickerStep)).y)];
            _fills.Add((new Rect2(markerLeft, markerTop, markerSide, markerSide), markerColor.Background));
        }

        /// <summary>
        /// Clusters shown at <paramref name="progress"/> through the cycle: typing in, holding, then erasing from
        /// the front. Typing and erasing share the time left after the hold.
        /// </summary>
        private static (int Tail, int Head) VisibleRange(float progress, float hold, int count)
        {
            var typeShare = Mathf.Max(0.01f, (1f - hold) / 2f);
            var head = progress < typeShare ? Mathf.FloorToInt(progress / typeShare * count) + 1 : count;
            var eraseStart = 1f - typeShare;
            var tail = progress > eraseStart ? Mathf.FloorToInt((progress - eraseStart) / typeShare * count) : 0;
            return (Math.Clamp(tail, 0, count), Math.Clamp(head, 0, count));
        }

        private void AddFill(int highlight, double originX, double left, double right, double baseline,
            double capHeight, double pad)
        {
            if (highlight < 0 || right <= left) return;

            var rect = Rect2.FromEdges(originX + left - pad, baseline - capHeight - pad, originX + right + pad,
                baseline + pad);
            _fills.Add((rect, Palette[highlight].Background));
        }

        /// <summary>Palette entry behind the word for this flicker step, or -1 when it has none.</summary>
        private static int HighlightIndex(uint seed, uint block, uint word, uint flickerStep, float chance)
        {
            var random = Pcg.Pcg01(new UInt4(seed, block, word, flickerStep));
            return random.x < chance ? PaletteIndex(random.y) : -1;
        }

        private static int PaletteIndex(float random) => Math.Min(Palette.Length - 1, (int)(random * Palette.Length));

        private readonly struct Highlight
        {
            public Highlight(Color background, bool darkText)
            {
                Background = background;
                DarkText = darkText;
            }

            public Color Background { get; }

            /// <summary>Black text reads on this background; otherwise white.</summary>
            public bool DarkText { get; }
        }

        private sealed class TextBatch
        {
            private readonly List<LayoutSlot> _slots = new();
            private readonly List<string> _texts = new();

            public void Clear()
            {
                _slots.Clear();
                _texts.Clear();
            }

            public void Add(LayoutSlot slot, string text)
            {
                _slots.Add(slot);
                _texts.Add(text);
            }

            public void Draw(TypeLayoutDrawer drawer, PlacementOptions placement, Color color)
            {
                if (_slots.Count > 0) drawer.DrawTexts(_slots, _texts, placement, color);
            }
        }
    }
}
