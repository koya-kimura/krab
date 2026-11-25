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

        /// <summary>The open preset's own Values. Their names are not read as "group/name".</summary>
        private const string ThisPresetTab = "This Preset";

        /// <summary>
        /// The tabs the Values lists show: This Preset first (when it has local Values), then the shared Values by
        /// their "group/name" path, as the Performance tabs group presets.
        /// </summary>
        private IReadOnlyList<(string Tab, IReadOnlyList<PresetValue> Values)> ValueTabsContent()
        {
            var tabs = new List<(string, IReadOnlyList<PresetValue>)>();
            if (Document.LocalValues.Count > 0) tabs.Add((ThisPresetTab, Document.LocalValues.ToList()));

            tabs.AddRange(PresetPathGroups.Of(Document.Values, value => value.Name).Select(group =>
                (TabLabel(group.Group), (IReadOnlyList<PresetValue>)group.Entries.Select(entry => entry.Item).ToList())));
            return tabs;
        }

        private static string TabLabel(string group) => group.Length == 0 ? UngroupedTab : group;

        private string TabOf(PresetValue value) =>
            Document.IsLocal(value.Id) ? ThisPresetTab : TabLabel(PresetPath.Parse(value.Name).Group);

        /// <summary>A shared Value shows the part after its group (the tab); a local Value shows its whole name.</summary>
        private string DisplayName(PresetValue value) =>
            Document.IsLocal(value.Id) ? value.Name : PresetPath.Parse(value.Name).Title;

        /// <summary>
        /// One tab per Value group. The tab named by <paramref name="tab"/> is selected when it still exists, and the
        /// user's tab choice is written back through <paramref name="setTab"/>.
        /// </summary>
        private Element ValueTabs(Func<IReadOnlyList<PresetValue>, Element> page, string tab, Action<string> setTab)
        {
            var content = ValueTabsContent();
            var labels = content.Select(entry => entry.Tab).ToList();
            var tabs = UI.Tabs(content.Select(entry => (entry.Tab, page(entry.Values))));
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
