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
                    PaneTitle("Values"),
                    UI.ScrollViewVertical(
                        400,
                        UI.DynamicElementOnStatusChanged(
                            () => (_documentRevision, _valuesRevision, _valueNamesRevision),
                            _ => BuildValueList())),
                    UI.Button("+ Value", AddValue)
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
            if (SelectedValue != null) _valuesTab = TabLabel(PresetPath.Parse(SelectedValue.Name).Group);
            return ValueTabs(
                entries => UI.Column(entries.Select(entry => ValueRow(entry.Item))),
                _valuesTab,
                tab => _valuesTab = tab);
        }

        private Element ValueRow(PresetValue value)
        {
            var row = UI.Button(UI.Label(() => PresetPath.Parse(value.Name).Title), () => SelectValue(value.Id));
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
                // "group/name" files the Value under a tab named after the group, as presets are.
                UI.Field("Name (group/name)", () => value.Name, name =>
                {
                    value.Name = name;
                    _valueNamesRevision++;
                    _dirty = true;
                }),
                PaneTitle("Components"),
                UI.Column(value.Components.Select(component => ComponentFold(value, component))),
                UI.Row(
                    UI.Button("+ ADSR", () => AddComponent(value, ModulatorKind.Adsr)),
                    UI.Button("+ LFO", () => AddComponent(value, ModulatorKind.Lfo)),
                    remove));
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
            Document.Values.Add(value);
            _valuesRevision++;
            SelectValue(value.Id);
            Changed();
        }

        /// <summary>"Value N" in the group of the open Values tab.</summary>
        private string NewValueName()
        {
            var group = GroupOfTab(_valuesTab);
            var title = "Value " + (Document.Values.Count + 1);
            return group.Length == 0 ? title : group + PresetPath.Separator + title;
        }

        private void DeleteValue(PresetValue value)
        {
            Guard(() =>
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
