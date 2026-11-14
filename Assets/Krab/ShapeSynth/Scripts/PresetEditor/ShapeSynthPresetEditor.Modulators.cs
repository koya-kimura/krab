using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Values tab: the Modulator library, its output graph and the selected signal's settings.</summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        private PresetSignalGraph _signalGraph;

        public PresetSignalGraph SignalGraph => _signalGraph;

        private void AddModulator(ModulatorKind kind)
        {
            SelectModulator(CreateModulator(kind).Id);
        }

        private ModulatorSettings CreateModulator(ModulatorKind kind)
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
            Changed();
            return model;
        }

        private int BindingCount(ModulatorSettings model)
        {
            return Document.Nodes()
                .SelectMany(node => node.Parameters())
                .SelectMany(parameter => parameter.Bindings)
                .Count(binding => binding.ModulatorId == model.Id);
        }

        private void DeleteModulator(ModulatorSettings model)
        {
            Guard(() =>
            {
                Document.DeleteModulator(model.Id);
                _modulatorsRevision++;
                _selectedModulatorId = Document.Modulators.FirstOrDefault()?.Id;
                Changed();
                _status = "Deleted " + model.Name;
            });
        }

        private Element BuildValuesView()
        {
            _signalGraph = new PresetSignalGraph(() => Document.Modulators, () => _selectedModulatorId, () => _beat);
            return UI.Row(BuildValueLibrary(), BuildValueWorkArea());
        }

        private Element BuildValueLibrary()
        {
            return UI.Column(
                UI.Column(
                    T.Text("Value Name", 10, T.Hex(0xaeb9c8)).Styled(style => style.marginBottom = 5),
                    UI.DynamicElementOnStatusChanged(
                        () => (_documentRevision, _selectedModulatorId, _modulatorsRevision),
                        _ => BuildValueNameField())
                ).SetFlexGrow(0).SetFlexShrink(0).Styled(style =>
                {
                    T.Padding(style, 12, 10);
                    T.Border(style, T.Stroke, 0, 0, 1, 0);
                }),
                T.PaneTitle("Value Library"),
                VerticalScroll(UI.DynamicElementOnStatusChanged(
                    () => (_documentRevision, _modulatorsRevision),
                    _ => UI.Column(Document.Modulators.Select(ModulatorRow)))).SetFlexGrow(1),
                T.ActionStrip(
                    T.Button("+ ADSR", () => AddModulator(ModulatorKind.Adsr)),
                    T.Button("+ LFO", () => AddModulator(ModulatorKind.Lfo)))
            ).SetWidth(240).SetMinWidth(180).SetFlexShrink(0).Fill(T.Panel)
                .Styled(style => T.Border(style, T.Stroke, 0, 1, 0, 0));
        }

        private Element BuildValueNameField()
        {
            var model = SelectedModulator;
            if (model == null) return T.Text("Add an ADSR or LFO.", 12, T.Muted);

            return UI.Field((LabelElement)null, () => model.Name, value =>
            {
                model.Name = value;
                _modulatorNamesRevision++;
                Changed(false);
            }).Styled(style => style.fontSize = 13);
        }

        private Element BuildValueWorkArea()
        {
            return UI.Column(
                UI.Column(
                    UI.Row(
                        T.Text("Output", 11, T.Hex(0xcbd3df), true),
                        UI.Space().SetFlexGrow(1),
                        T.Text("0–6 beats · line = Beat", 11, T.Muted)
                    ).Styled(style => style.marginBottom = 7),
                    _signalGraph.CreateElement(),
                    UI.Row(
                        Legend("Selected", T.Sum),
                        Legend("ADSR", T.Adsr),
                        Legend("LFO", T.Lfo),
                        UI.Space().SetFlexGrow(1),
                        T.Text(() => $"Beat {_beat:F2} · {DisplayedSelectedValue()}", 10, T.Output)
                    ).Styled(style =>
                    {
                        style.marginTop = 6;
                        style.alignItems = Align.Center;
                    })
                ).SetFlexGrow(0).SetFlexShrink(0).Styled(style =>
                {
                    T.Padding(style, 11, 14);
                    T.Border(style, T.Stroke, 0, 0, 1, 0);
                }),
                T.PaneTitle("Signal Components"),
                VerticalScroll(UI.DynamicElementOnStatusChanged(
                    () => (_documentRevision, _selectedModulatorId, _modulatorsRevision),
                    _ => BuildSignalCard(SelectedModulator))).SetFlexGrow(1)
            ).SetFlexGrow(1).SetFlexShrink(1).SetMinWidth(0).Fill(T.Background);
        }

        private string DisplayedSelectedValue()
        {
            var model = SelectedModulator;
            return model == null ? "—" : "value " + PresetSignalSampler.SampleOrZero(model, _beat).ToString("F2");
        }

        private static Element Legend(string label, Color color)
        {
            return UI.Row(
                UI.Space().SetWidth(15).SetHeight(2).Fill(color).Styled(style =>
                {
                    style.marginRight = 4;
                    style.alignSelf = Align.Center;
                }),
                T.Text(label, 10, T.Hex(0x8c98a8))
            ).SetFlexGrow(0).Styled(style =>
            {
                style.marginRight = 16;
                style.alignItems = Align.Center;
            });
        }

        private Element ModulatorRow(ModulatorSettings model)
        {
            var row = UI.Row(
                T.Text("●", 10, model.Kind == ModulatorKind.Adsr ? T.Adsr : T.Lfo).SetWidth(18),
                UI.Button(T.Text(() => model.Name, 12), () => SelectModulator(model.Id))
                    .SetFlexGrow(1).Styled(RowTitleCell),
                T.Text(model.Kind == ModulatorKind.Adsr ? "ADSR" : "LFO", 10, T.Muted));
            return PresetButtonStyle.Selectable(row, () => model.Id == _selectedModulatorId).Styled(style =>
            {
                style.height = 30;
                style.alignItems = Align.Center;
                T.Padding(style, 0, 11);
            });
        }

        private Element BuildSignalCard(ModulatorSettings model)
        {
            if (model == null) return T.Text("Select a Value in the library.", 12, T.Muted).Styled(style => T.Padding(style, 12, 14));

            var adsr = model.Kind == ModulatorKind.Adsr;
            var fields = adsr
                ? new[]
                {
                    Number("Attack (beats)", () => model.Attack, value => model.Attack = value),
                    Number("Decay (beats)", () => model.Decay, value => model.Decay = value),
                    Number("Sustain Level", () => model.SustainLevel, value => model.SustainLevel = value),
                    Number("Sustain Time", () => model.SustainTime, value => model.SustainTime = value),
                    Number("Release (beats)", () => model.Release, value => model.Release = value),
                }
                : new[]
                {
                    T.Labeled("Waveform", Choice(null, () => (int)model.Waveform, value =>
                    {
                        model.Waveform = (LfoWaveform)value;
                        Changed();
                    }, Enum.GetNames(typeof(LfoWaveform)))).SetFlexGrow(1).SetFlexBasis(0),
                    Number("Cycles / Beat", () => model.CyclesPerBeat, value => model.CyclesPerBeat = value),
                    Number("Phase Offset", () => model.PhaseOffset, value => model.PhaseOffset = value),
                };
            foreach (var field in fields)
            {
                field.SetMinWidth(92).Styled(style =>
                {
                    style.marginRight = 8;
                    style.marginBottom = 8;
                });
            }

            var description = adsr
                ? "Durations are in beats. Sustain Level 0–1. An ADSR also defines the Voice lifetime."
                : "Output −1…1. Phase Offset is in cycles.";
            return UI.Row(
                UI.Column(
                    T.Text(adsr ? "ADSR" : "LFO", 12, T.Foreground, true),
                    T.Text(adsr ? "Envelope · Lifetime" : "Oscillator", 10, T.Muted).Styled(style => style.marginTop = 4),
                    T.Text(() => BindingCount(model) == 0 ? "Unused" : "Bound × " + BindingCount(model), 10, T.Output)
                        .Styled(style => style.marginTop = 8),
                    UI.Space().SetFlexGrow(1),
                    EnabledButton("Remove", () => DeleteModulator(model), () => BindingCount(model) == 0).Styled(SmallButton)
                ).SetWidth(127).SetFlexShrink(0).Styled(style =>
                {
                    T.Padding(style, 10, 10);
                    T.Border(style, T.Stroke, 0, 1, 0, 0);
                }),
                UI.Column(
                    UI.Row(fields).SetFlexWrap(true),
                    T.Text(description, 10, T.Muted).Styled(style => style.whiteSpace = WhiteSpace.Normal),
                    T.Text(
                        () => BindingCount(model) > 0 ? "In use: remove its Bindings in the Visual Inspector before removing." : "",
                        10,
                        T.Warning).Styled(style =>
                    {
                        style.marginTop = 4;
                        style.whiteSpace = WhiteSpace.Normal;
                    }),
                    T.Text("ID " + model.Id, 10, T.Hex(0x6c7788)).Styled(style => style.marginTop = 6)
                ).SetFlexGrow(1).SetMinWidth(0).Styled(style => T.Padding(style, 11, 11))
            ).Fill(T.Raised).Styled(style =>
            {
                T.Border(style, T.Hex(0x414956));
                style.marginTop = 8;
                style.marginBottom = 10;
                style.marginLeft = 14;
                style.marginRight = 14;
            });
        }
    }
}
