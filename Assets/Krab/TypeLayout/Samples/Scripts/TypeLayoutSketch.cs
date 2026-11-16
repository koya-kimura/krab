using System.Collections.Generic;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using Krab.TypeLayout.Core.Text;
using Krab.TypeLayout.Unity.Rendering;
using Unite.Canvas;
using Unite.Text.Fonts;
using UnityEngine;

namespace Krab.TypeLayout.Samples
{
    /// <summary>Glue for the samples: runs Core layouts and draws them, optionally with guides.</summary>
    public sealed class TypeLayoutSketch
    {
        private static readonly Color PathGuideColor = new(1f, 0.85f, 0.2f, 0.6f);
        private static readonly Color AnchorGuideColor = new(1f, 0.2f, 0.8f, 0.9f);

        private readonly UCanvas _canvas;
        private readonly FontFace _font;
        private readonly List<FlowItem> _flowItems = new();
        private readonly List<LayoutSlot> _slots = new();
        private readonly List<string> _texts = new();
        private readonly List<GlyphPlacement> _placements = new();

        public TypeLayoutSketch(UCanvas canvas, FontFace font)
        {
            _canvas = canvas;
            _font = font;
            Metrics = new UniteGlyphMetrics(canvas, font);
        }

        public UniteGlyphMetrics Metrics { get; }

        public bool ShowGuides { get; set; }

        /// <summary>One text per slot, repeating the text's clusters in order.</summary>
        public void DrawSlots(IReadOnlyList<LayoutSlot> slots, string text, PlacementOptions placement, Color color)
        {
            TextSequence.Cycle(TextClusters.Split(text), slots.Count, 0, _texts);
            DrawPlaced(slots, placement, color);
        }

        /// <summary>One given text per slot (a text may hold several characters, such as "12").</summary>
        public void DrawSlots(IReadOnlyList<LayoutSlot> slots, IReadOnlyList<string> texts, PlacementOptions placement,
            Color color)
        {
            _texts.Clear();
            _texts.AddRange(texts);
            DrawPlaced(slots, placement, color);
        }

        public void DrawFlow(ILayoutPath path, string text, FlowOptions flow, PlacementOptions placement, Color color)
        {
            var clusters = TextClusters.Split(text);
            var glyphs = new FlowGlyph[clusters.Length];
            for (var i = 0; i < clusters.Length; i++)
                glyphs[i] = FlowGlyph.FromMeasure(Metrics.Measure(clusters[i]), placement.Size);

            FlowDistributor.Flow(path, glyphs, flow, _flowItems);
            _slots.Clear();
            _texts.Clear();
            foreach (var item in _flowItems)
            {
                _slots.Add(item.Slot);
                _texts.Add(clusters[item.ClusterIndex]);
            }

            if (ShowGuides) UniteGuideRenderer.DrawPath(_canvas, path, PathGuideColor);

            DrawPlaced(_slots, placement, color);
        }

        public void DrawPathGuide(ILayoutPath path)
        {
            if (ShowGuides) UniteGuideRenderer.DrawPath(_canvas, path, PathGuideColor);
        }

        private void DrawPlaced(IReadOnlyList<LayoutSlot> slots, PlacementOptions placement, Color color)
        {
            GlyphPlacer.Place(slots, _texts, Metrics, placement, _placements);
            UniteTextRenderer.Draw(_canvas, _font, _placements, color);
            if (ShowGuides) UniteGuideRenderer.DrawAnchors(_canvas, _placements, AnchorGuideColor);
        }
    }
}
