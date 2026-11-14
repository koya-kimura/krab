using System;
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

        private PresetSignalGraph _signalGraph;
        private int _modulatorNamesRevision;

        private Element BuildValuesView()
        {
            _signalGraph = new PresetSignalGraph(() => Document.Modulators, () => _selectedModulatorId, () => _beat);
            return UI.Row(
                UI.Column(
                    UI.ScrollViewVertical(
                        400,
                        UI.DynamicElementOnStatusChanged(
                            () => (_documentRevision, _modulatorsRevision),
                            _ => UI.Column(Document.Modulators.Select(ModulatorRow)))),
                    UI.Row(
                        UI.Button("+ ADSR", () => AddModulator(ModulatorKind.Adsr)),
                        UI.Button("+ LFO", () => AddModulator(ModulatorKind.Lfo)))
                ).SetWidth(220),
                UI.Column(
                    _signalGraph.CreateElement(),
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedModulatorId, _modulatorsRevision),
                        _ => BuildModulatorFields(SelectedModulator)))
                    .SetWidth(520));
        }

        private Element ModulatorRow(ModulatorSettings model)
        {
            var kind = model.Kind == ModulatorKind.Adsr ? "ADSR" : "LFO";
            return UI.Button(
                UI.Label(() => (model.Id == _selectedModulatorId ? "> " : "") + model.Name + "  (" + kind + ")"),
                () => SelectModulator(model.Id));
        }

        private Element BuildModulatorFields(ModulatorSettings model)
        {
            if (model == null) return UI.Space();

            var nameField = UI.Field("Name", () => model.Name, value =>
            {
                model.Name = value;
                _modulatorNamesRevision++;
                _dirty = true;
            });
            var remove = UI.Button("Remove", () => DeleteModulator(model));
            remove.RegisterUpdateCallback(element => element.Interactable = BindingCount(model) == 0);
            if (model.Kind == ModulatorKind.Adsr)
            {
                return UI.Column(
                    nameField,
                    NumberField("Attack", () => model.Attack, value => model.Attack = value),
                    NumberField("Decay", () => model.Decay, value => model.Decay = value),
                    NumberField("Sustain Level", () => model.SustainLevel, value => model.SustainLevel = value),
                    NumberField("Sustain Time", () => model.SustainTime, value => model.SustainTime = value),
                    NumberField("Release", () => model.Release, value => model.Release = value),
                    remove);
            }

            return UI.Column(
                nameField,
                Choice("Waveform", () => (int)model.Waveform, value =>
                {
                    model.Waveform = (LfoWaveform)value;
                    Changed();
                }, WaveformNames),
                NumberField("Cycles / Beat", () => model.CyclesPerBeat, value => model.CyclesPerBeat = value),
                NumberField("Phase Offset", () => model.PhaseOffset, value => model.PhaseOffset = value),
                remove);
        }

        private void AddModulator(ModulatorKind kind)
        {
            var model = new ModulatorSettings
            {
                Id = PresetDocument.NewId(),
                Name = "New " + (kind == ModulatorKind.Adsr ? "ADSR" : "LFO"),
                Kind = kind,
                Attack = 0.1,
                Decay = 0.2,
                SustainLevel = 0.8,
                SustainTime = 1,
                Release = 1,
                CyclesPerBeat = 0.5,
            };
            Document.Modulators.Add(model);
            _modulatorsRevision++;
            SelectModulator(model.Id);
            Changed();
        }

        private void DeleteModulator(ModulatorSettings model)
        {
            Guard(() =>
            {
                Document.DeleteModulator(model.Id);
                _modulatorsRevision++;
                _selectedModulatorId = Document.Modulators.FirstOrDefault()?.Id;
                Changed();
            });
        }

        private int BindingCount(ModulatorSettings model)
        {
            return Document.Nodes()
                .SelectMany(node => node.Parameters())
                .SelectMany(parameter => parameter.Bindings)
                .Count(binding => binding.ModulatorId == model.Id);
        }
    }
}
