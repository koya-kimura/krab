using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const string UngroupedTab = "Ungrouped";

        /// <summary>Shared Values by their "group/name" path, as the Performance tabs group presets.</summary>
        private IReadOnlyList<(string Group, IReadOnlyList<(PresetPath Path, PresetValue Item)> Entries)> ValueGroups() =>
            PresetPathGroups.Of(Document.Values, value => value.Name);

        /// <summary>Values in group order, for lists that are not split into tabs (the Bind dropdown).</summary>
        private PresetValue[] ValuesInGroupOrder() =>
            ValueGroups().SelectMany(group => group.Entries.Select(entry => entry.Item)).ToArray();

        private static string TabLabel(string group) => group.Length == 0 ? UngroupedTab : group;

        private static string GroupOfTab(string label) => label == UngroupedTab ? string.Empty : label ?? string.Empty;

        /// <summary>
        /// One tab per Value group. The tab named by <paramref name="tab"/> is selected when it still exists, and the
        /// user's tab choice is written back through <paramref name="setTab"/>.
        /// </summary>
        private Element ValueTabs(
            Func<IReadOnlyList<(PresetPath Path, PresetValue Item)>, Element> page,
            string tab,
            Action<string> setTab)
        {
            var groups = ValueGroups();
            var labels = groups.Select(group => TabLabel(group.Group)).ToList();
            var tabs = UI.Tabs(groups.Select(group => (TabLabel(group.Group), page(group.Entries))));
            var selected = labels.IndexOf(tab);
            if (selected >= 0) tabs.CurrentTabIndex = selected;

            tabs.RegisterUpdateCallback(_ =>
            {
                var index = tabs.CurrentTabIndex;
                if (index >= 0 && index < labels.Count) setTab(labels[index]);
            });
            return tabs;
        }
    }
}
