using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private static readonly string[] WaveformNames = Enum.GetNames(typeof(LfoWaveform));

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
                    remove));
        }

        private Element ComponentFold(PresetValue value, PresetComponent component)
        {
            var adsr = component.Kind == ModulatorKind.Adsr;
            var header = UI.Label(() => $"{(adsr ? "● ADSR" : "◆ LFO")}  × {component.Weight:0.##}")
                .SetColor(adsr ? PresetEditorPalette.Adsr : PresetEditorPalette.Lfo);
            return UI.Fold(header, ComponentFields(value, component, adsr)).Open();
        }

        private IEnumerable<Element> ComponentFields(PresetValue value, PresetComponent component, bool adsr)
        {
            yield return NumberField("Weight", () => component.Weight, weight => component.Weight = weight);
            if (adsr)
            {
                yield return NumberField("Attack", () => component.Attack, beats => component.Attack = beats);
                yield return NumberField("Decay", () => component.Decay, beats => component.Decay = beats);
                yield return NumberField("Sustain Level", () => component.SustainLevel, level => component.SustainLevel = level);
                yield return NumberField("Sustain Time", () => component.SustainTime, beats => component.SustainTime = beats);
                yield return NumberField("Release", () => component.Release, beats => component.Release = beats);
            }
            else
            {
                yield return Choice("Waveform", () => (int)component.Waveform, index =>
                {
                    component.Waveform = (LfoWaveform)index;
                    Changed();
                }, WaveformNames);
                yield return NumberField("Cycles / Beat", () => component.CyclesPerBeat, rate => component.CyclesPerBeat = rate);
                yield return NumberField("Phase Offset", () => component.PhaseOffset, phase => component.PhaseOffset = phase);
            }

            yield return UI.Button("Remove Component", () => RemoveComponent(value, component));
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
