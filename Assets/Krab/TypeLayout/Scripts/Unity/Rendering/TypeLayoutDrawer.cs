using System.Collections.Generic;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Krab.TypeLayout.Core.Text;
using Unite.Canvas;
using Unite.Text.Fonts;
using UnityEngine;

namespace Krab.TypeLayout.Unity.Rendering
{
    /// <summary>Runs Core layouts and draws them with one font, optionally with guides. Reuses its buffers.</summary>
    public sealed class TypeLayoutDrawer
    {
        // Live text edits create a new entry per keystroke; dropping the cache now and then bounds it.
        private const int MaxCachedTexts = 64;

        private static readonly Color PathGuideColor = new(1f, 0.85f, 0.2f, 0.6f);
        private static readonly Color AnchorGuideColor = new(1f, 0.2f, 0.8f, 0.9f);

        private readonly UCanvas _canvas;
        private readonly FontFace _font;
        private readonly Dictionary<string, string[]> _clusters = new();
        private readonly Dictionary<(string Text, double Size), FlowGlyph[]> _glyphs = new();
        private readonly List<FlowItem> _flowItems = new();
        private readonly List<LayoutSlot> _slots = new();
        private readonly List<string> _texts = new();
        private readonly List<GlyphPlacement> _placements = new();

        public TypeLayoutDrawer(UCanvas canvas, FontFace font)
        {
            _canvas = canvas;
            _font = font;
            Metrics = new UniteGlyphMetrics(canvas, font);
        }

        public UniteGlyphMetrics Metrics { get; }

        public bool ShowGuides { get; set; }

        public IReadOnlyList<string> Clusters(string text)
        {
            text ??= string.Empty;
            if (_clusters.TryGetValue(text, out var clusters)) return clusters;

            if (_clusters.Count >= MaxCachedTexts) _clusters.Clear();

            clusters = TextClusters.Split(text);
            _clusters.Add(text, clusters);
            return clusters;
        }

        /// <summary>One text per slot, repeating the text's clusters from <paramref name="start"/>.</summary>
        public void DrawSequence(IReadOnlyList<LayoutSlot> slots, string text, int start, PlacementOptions placement,
            Color color)
        {
            var clusters = Clusters(text);
            if (clusters.Count == 0) return;

            TextSequence.Cycle(clusters, slots.Count, start, _texts);
            DrawPlaced(slots, placement, color);
        }

        /// <summary>One seeded random cluster of the text per slot; a new step re-rolls them.</summary>
        public void DrawRandom(IReadOnlyList<LayoutSlot> slots, string text, int seed, long step,
            PlacementOptions placement, Color color)
        {
            var clusters = Clusters(text);
            if (clusters.Count == 0) return;

            TextPicker.Random(clusters, slots.Count, seed, step, _texts);
            DrawPlaced(slots, placement, color);
        }

        /// <summary>One given text per slot (a text may hold several characters, such as "12").</summary>
        public void DrawTexts(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<string> texts, PlacementOptions placement,
            Color color)
        {
            _texts.Clear();
            for (var i = 0; i < texts.Count; i++)
                _texts.Add(texts[i]);

            DrawPlaced(slots, placement, color);
        }

        public void DrawFlow(ILayoutPath path, string text, FlowOptions flow, PlacementOptions placement, Color color)
        {
            DrawPathGuide(path);
            var clusters = Clusters(text);
            if (clusters.Count == 0) return;

            FlowDistributor.Flow(path, Glyphs(text, clusters, placement.Size), flow, _flowItems);
            _slots.Clear();
            _texts.Clear();
            foreach (var item in _flowItems)
            {
                _slots.Add(item.Slot);
                _texts.Add(clusters[item.ClusterIndex]);
            }

            DrawPlaced(_slots, placement, color);
        }

        /// <summary>A filled rectangle without outline, such as a highlight behind text.</summary>
        public void FillRect(Rect2 rect, Color color)
        {
            _canvas.Push();
            try
            {
                _canvas.NoStroke();
                _canvas.Fill(color);
                _canvas.Rect((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
            }
            finally
            {
                _canvas.Pop();
            }
        }

        public void DrawPathGuide(ILayoutPath path)
        {
            if (ShowGuides) UniteGuideRenderer.DrawPath(_canvas, path, PathGuideColor);
        }

        private FlowGlyph[] Glyphs(string text, IReadOnlyList<string> clusters, double size)
        {
            if (_glyphs.TryGetValue((text, size), out var glyphs)) return glyphs;

            if (_glyphs.Count >= MaxCachedTexts) _glyphs.Clear();

            glyphs = new FlowGlyph[clusters.Count];
            for (var i = 0; i < clusters.Count; i++)
                glyphs[i] = FlowGlyph.FromMeasure(Metrics.Measure(clusters[i]), size);

            _glyphs.Add((text, size), glyphs);
            return glyphs;
        }

        private void DrawPlaced(IReadOnlyList<LayoutSlot> slots, PlacementOptions placement, Color color)
        {
            GlyphPlacer.Place(slots, _texts, Metrics, placement, _placements);
            UniteTextRenderer.Draw(_canvas, _font, _placements, color);
            if (ShowGuides) UniteGuideRenderer.DrawAnchors(_canvas, _placements, AnchorGuideColor);
        }
    }
}
