using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const int ValueGalleryColumns = 6;
        private const float ValueGalleryCellWidth = 180;
        private const float ValueGalleryGraphHeight = 90;
        private const float ValueGalleryGap = 16;

        /// <summary>
        /// Every Value as a small signal graph with its name, like Gallery does for presets, grouped by
        /// "group/name". Graphs follow edits. Like every Value graph, the Beat cursor follows the Visual
        /// window's playback, so all the graphs and the preview show the same moment.
        /// </summary>
        private Element BuildValueGalleryWindow()
        {
            return Scrollable(
                BuildValueMessages(),
                UI.DynamicElementOnStatusChanged(
                    () => (_documentRevision, _valuesRevision, _valueNamesRevision),
                    _ => BuildValueGallerySections()));
        }

        /// <summary>
        /// Every group at once, one after another under its name, so the whole library can be looked over without
        /// switching tabs. The order is the tab order of the Values window.
        /// </summary>
        private Element BuildValueGallerySections()
        {
            if (!Document.AllValues().Any()) return UI.Label("No values.");

            return UI.Column(Gapped(ValueTabsContent().Select(section => (Element)UI.Column(
                    PaneTitle(section.Tab),
                    BuildValueGalleryGrid(section.Values, ValueGalleryCell))),
                () => UI.Space().SetHeight(ValueGalleryGap * 2)));
        }

        /// <summary>Values in rows of six with gaps; Value Gallery and the Bind picker share it.</summary>
        private static Element BuildValueGalleryGrid(
            IReadOnlyList<PresetValue> values, Func<PresetValue, Element> cell, float gap = ValueGalleryGap)
        {
            var rows = (values.Count + ValueGalleryColumns - 1) / ValueGalleryColumns;
            return UI.Column(Gapped(Enumerable.Range(0, rows).Select(row =>
                (Element)UI.Row(Gapped(values.Skip(row * ValueGalleryColumns).Take(ValueGalleryColumns)
                    .Select(cell), () => UI.Space().SetWidth(gap)))),
                () => UI.Space().SetHeight(gap)));
        }

        // RosettaUI has no margin setter, so spacers sit between the cells and between the rows.
        private static IEnumerable<Element> Gapped(IEnumerable<Element> elements, Func<Element> gap)
        {
            var first = true;
            foreach (var element in elements)
            {
                if (!first) yield return gap().SetFlexShrink(0);
                first = false;
                yield return element;
            }
        }

        private Element ValueGalleryCell(PresetValue value)
        {
            var graph = new PresetSignalGraph(() => value, () => _beat, ValueGalleryGraphHeight);
            return UI.Column(graph.CreateElement(), UI.Label(() => DisplayName(value))).SetWidth(ValueGalleryCellWidth);
        }
    }
}
