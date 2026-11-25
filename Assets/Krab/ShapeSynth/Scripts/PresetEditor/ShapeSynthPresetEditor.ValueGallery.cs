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

        private string _valueGalleryTab;

        /// <summary>
        /// Every shared Value as a small signal graph with its name, like Gallery does for presets, in one tab per
        /// "group/name" group. Graphs follow edits. Like every Value graph, the Beat cursor follows the Visual
        /// window's playback, so all the graphs and the preview show the same moment.
        /// </summary>
        private Element BuildValueGalleryWindow()
        {
            return Scrollable(
                BuildValueMessages(),
                UI.DynamicElementOnStatusChanged(
                    () => (_documentRevision, _valuesRevision, _valueNamesRevision),
                    _ => BuildValueGalleryTabs()));
        }

        /// <summary>One tab per Value group, as the Values window and the Performance tabs.</summary>
        private Element BuildValueGalleryTabs()
        {
            if (!Document.AllValues().Any()) return UI.Label("No values.");

            return ValueTabs(
                values => BuildValueGalleryGrid(values, ValueGalleryCell),
                _valueGalleryTab,
                tab => _valueGalleryTab = tab);
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
