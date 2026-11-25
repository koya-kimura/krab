using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {

        private int _valueNamesRevision;
        private string _valuesTab;

        private Element BuildValuesView()
        {
            var signalGraph = new PresetSignalGraph(() => SelectedValue, () => _beat);
            return UI.Row(
                UI.Column(
                    // Adding sits beside the title so it stays in reach however long the list grows.
                    UI.Row(PaneTitle("Values"), UI.Button("+ Value", AddValue)),
                    UI.ScrollViewVertical(
                        400,
                        UI.DynamicElementOnStatusChanged(
                            () => (_documentRevision, _valuesRevision, _valueNamesRevision),
                            _ => BuildValueList()))
                ).SetWidth(200),
                UI.Column(
                    PaneTitle("Output"),
                    signalGraph.CreateElement(),
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedValueId, _valuesRevision),
                        _ => BuildValueFields(SelectedValue)))
                    .SetWidth(520));
        }

        /// <summary>Tabs per Value group. A rebuild after an edit shows the group of the selected Value.</summary>
        private Element BuildValueList()
        {
            if (SelectedValue != null) _valuesTab = TabOf(SelectedValue);
            return ValueTabs(
                values => UI.Column(values.Select(ValueRow)),
                _valuesTab,
                tab => _valuesTab = tab);
        }

        private Element ValueRow(PresetValue value)
        {
            var row = UI.Button(UI.Label(() => DisplayName(value)), () => SelectValue(value.Id));
            row.RegisterUpdateCallback(element =>
                element.Style.BackgroundColor = value.Id == _selectedValueId ? PresetEditorPalette.Selected : null);
            return row;
        }

        private Element BuildValueFields(PresetValue value)
        {
            if (value == null) return UI.Space();

            var remove = UI.Button("Remove Value", () => DeleteValue(value));
            remove.RegisterUpdateCallback(element => element.Interactable = !IsValueInUse(value.Id));
            return UI.Column(
                // Removing acts on the whole Value, so it sits with its name rather than with the Component buttons.
                UI.Row(
                    // "group/name" files the Value under a tab named after the group, as presets are.
                    UI.Field("Name (group/name)", () => value.Name, name =>
                    {
                        value.Name = name;
                        _valueNamesRevision++;
                        _dirty = true;
                    }).SetFlexGrow(1),
                    UI.Button("Duplicate", () => DuplicateValue(value)),
                    remove),
                ValueScopeLine(value),
                PaneTitle("Components"),
                UI.Column(value.Components.Select(component => ComponentFold(value, component))),
                UI.Row(
                    UI.Button("+ ADSR", () => AddComponent(value, ModulatorKind.Adsr)),
                    UI.Button("+ LFO", () => AddComponent(value, ModulatorKind.Lfo))));
        }

        private Element ComponentFold(PresetValue value, PresetComponent component)
        {
            var header = UI.Label(() => $"{ComponentTitle(component.Kind)}  × {component.Weight:0.##}")
                .SetColor(PresetEditorPalette.Modulator(component.Kind));
            return UI.Fold(header, ComponentFields(value, component)).Open();
        }

        private static string ComponentTitle(ModulatorKind kind)
        {
            switch (kind)
            {
                case ModulatorKind.Adsr:
                    return "● ADSR";
                default:
                    return "◆ LFO";
            }
        }

        private void AddValue()
        {
            var value = new PresetValue
            {
                Id = PresetDocument.NewId(),
                Name = NewValueName(),
                Components = new List<PresetComponent> { PresetComponent.Create(ModulatorKind.Adsr) }
            };
            // The This Preset tab adds a local Value; any other tab adds to the shared library.
            (_valuesTab == ThisPresetTab ? Document.LocalValues : Document.Values).Add(value);
            _valuesRevision++;
            SelectValue(value.Id);
            Changed();
        }

        /// <summary>
        /// "New Value", numbered when taken. It has no group, so it lands in Ungrouped until it is renamed.
        /// </summary>
        private string NewValueName() => UniqueName("New Value", Document.AllValues().Select(value => value.Name));

        /// <summary>
        /// Adds a copy named "… Copy" right after the original and selects it. A shared Value is copied into the
        /// library, a local one stays local.
        /// </summary>
        private void DuplicateValue(PresetValue value)
        {
            GuardValues(() =>
            {
                var copy = value.Duplicate(PresetDocument.NewId(),
                    UniqueName(value.Name + " Copy", Document.AllValues().Select(other => other.Name)));
                var list = Document.IsLocal(value.Id) ? Document.LocalValues : Document.Values;
                list.Insert(list.IndexOf(value) + 1, copy);
                _valuesRevision++;
                SelectValue(copy.Id);
                Changed();
            });
        }

        private void DeleteValue(PresetValue value)
        {
            GuardValues(() =>
            {
                var user = SavedPresetUsing(value.Id);
                if (user != null) throw new ArgumentException($"Value is used by the preset '{user.Name}'.");

                Document.DeleteValue(value.Id);
                _valuesRevision++;
                _selectedValueId = Document.Values.FirstOrDefault()?.Id;
                Changed();
            });
        }

        /// <summary>Values are shared, so one bound by the open preset or by any saved preset stays.</summary>
        private bool IsValueInUse(string id) => Document.BindingCount(id) > 0 || SavedPresetUsing(id) != null;

        /// <summary>
        /// Says whose Value this is: a local one changes only the open preset; a shared one lists every preset
        /// it would change, so an edit here is not a surprise.
        /// </summary>
        private Element ValueScopeLine(PresetValue value)
        {
            return UI.Label(() =>
            {
                if (Document.IsLocal(value.Id)) return "This preset only.";

                var users = PresetsUsing(value.Id);
                return users.Count == 0
                    ? "Shared. Not used by any preset."
                    : $"Shared. Editing changes {users.Count} preset(s): {string.Join(", ", users)}";
            }).SetColor(PresetEditorPalette.Title);
        }

        /// <summary>Names of the presets binding the Value: the open one (saved or not) and the other saved ones.</summary>
        private List<string> PresetsUsing(string id)
        {
            var names = _savedPresets.Where(preset => preset.Id != Document.Id && preset.BindingCount(id) > 0)
                .Select(preset => preset.Name).ToList();
            if (Document.BindingCount(id) > 0) names.Insert(0, Document.Name);
            return names;
        }

        /// <summary>A saved preset other than the open one that binds the Value, or null.</summary>
        private PresetDocument SavedPresetUsing(string id) =>
            _savedPresets.FirstOrDefault(preset => preset.Id != Document.Id && preset.BindingCount(id) > 0);

        private void AddComponent(PresetValue value, ModulatorKind kind)
        {
            value.Components.Add(PresetComponent.Create(kind));
            _valuesRevision++;
            Changed();
        }

        private void RemoveComponent(PresetValue value, PresetComponent component)
        {
            value.Components.Remove(component);
            _valuesRevision++;
            Changed();
        }
    }
}
