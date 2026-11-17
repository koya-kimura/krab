using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {

        private int _valueNamesRevision;

        private Element BuildValuesView()
        {
            var signalGraph = new PresetSignalGraph(() => SelectedValue, () => _beat);
            return UI.Row(
                UI.Column(
                    PaneTitle("Values"),
                    UI.ScrollViewVertical(
                        400,
                        UI.DynamicElementOnStatusChanged(
                            () => (_documentRevision, _valuesRevision),
                            _ => UI.Column(Document.Values.Select(ValueRow)))),
                    UI.Button("+ Value", AddValue)
                ).SetWidth(220),
                UI.Column(
                    PaneTitle("Output"),
                    signalGraph.CreateElement(),
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedValueId, _valuesRevision),
                        _ => BuildValueFields(SelectedValue)))
                    .SetWidth(520));
        }

        private Element ValueRow(PresetValue value)
        {
            var row = UI.Button(UI.Label(() => value.Name), () => SelectValue(value.Id));
            row.RegisterUpdateCallback(element =>
                element.Style.BackgroundColor = value.Id == _selectedValueId ? PresetEditorPalette.Selected : null);
            return row;
        }

        private Element BuildValueFields(PresetValue value)
        {
            if (value == null) return UI.Space();

            var remove = UI.Button("Remove Value", () => DeleteValue(value));
            remove.RegisterUpdateCallback(element => element.Interactable = Document.BindingCount(value.Id) == 0);
            return UI.Column(
                UI.Field("Name", () => value.Name, name =>
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
                    UI.Button("+ Ramp", () => AddComponent(value, ModulatorKind.Ramp)),
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
                case ModulatorKind.Ramp:
                    return "▲ Ramp";
                default:
                    return "◆ LFO";
            }
        }

        private void AddValue()
        {
            var value = new PresetValue
            {
                Id = PresetDocument.NewId(),
                Name = "Value " + (Document.Values.Count + 1),
                Components = new List<PresetComponent> { PresetComponent.Create(ModulatorKind.Adsr) }
            };
            Document.Values.Add(value);
            _valuesRevision++;
            SelectValue(value.Id);
            Changed();
        }

        private void DeleteValue(PresetValue value)
        {
            Guard(() =>
            {
                Document.DeleteValue(value.Id);
                _valuesRevision++;
                _selectedValueId = Document.Values.FirstOrDefault()?.Id;
                Changed();
            });
        }

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
