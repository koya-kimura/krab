using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using UnityEngine.UIElements;
using T = Krab.ShapeSynth.PresetEditor.PresetEditorTheme;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private readonly Dictionary<PresetParameter, string> _pendingSignals = new();
        private readonly Dictionary<PresetParameter, double> _pendingDepths = new();

        private PresetParameter _editingParameter;

        /// <summary>Name, live output, Base / Reference / Bind, and an inline binding editor when opened.</summary>
        private Element ParameterRow(string name, PresetParameter parameter)
        {
            return UI.Column(
                UI.Row(
                    T.Text(name, 12, T.Foreground, true),
                    UI.Space().SetFlexGrow(1),
                    T.Text(() => ParameterOutput(parameter), 11, T.Output)
                ).Styled(style =>
                {
                    style.alignItems = Align.Center;
                    style.marginBottom = 4;
                }),
                UI.Row(
                    T.Labeled("Base", NumberInput(() => parameter.Base, value => parameter.Base = value))
                        .SetWidth(78).SetFlexShrink(0),
                    T.Labeled("Reference", Choice(null, () => (int)parameter.Reference, value =>
                    {
                        parameter.Reference = (ReferenceSource)value;
                        Changed();
                    }, Enum.GetNames(typeof(ReferenceSource)))).SetFlexGrow(1).SetFlexShrink(1),
                    BindButton(name, parameter).SetWidth(96).SetFlexShrink(0)
                ).Styled(style => style.alignItems = Align.FlexEnd),
                UI.DynamicElementIf(() => _editingParameter == parameter, () => BindDetails(name, parameter))
            ).Styled(style =>
            {
                T.Padding(style, 8, 0);
                T.Border(style, T.Hex(0x303640), 0, 0, 1, 0);
            });
        }

        private Element BindButton(string name, PresetParameter parameter)
        {
            // Green = bound, blue = its editor is open, grey = unbound; hover/press come from PresetButtonStyle.
            var button = PresetButtonStyle.Create(
                T.Text(() => BindLabel(parameter), 10),
                () => ToggleBinding(name, parameter),
                primary: () => _editingParameter == parameter && parameter.Bindings.Count == 0,
                active: () => parameter.Bindings.Count > 0);
            return button.Styled(style =>
            {
                T.Radius(style, 3);
                T.Padding(style, 4, 3);
                T.Margin(style, 0, 0);
                style.marginLeft = 5;
                style.minHeight = 24;
                style.height = 26;
                style.alignSelf = Align.FlexEnd;
                style.overflow = Overflow.Hidden;
            });
        }

        private string BindLabel(PresetParameter parameter)
        {
            if (parameter.Bindings.Count == 0) return _editingParameter == parameter ? "▾ Bind" : "+ Bind";

            var first = Document.Modulators.FirstOrDefault(model => model.Id == parameter.Bindings[0].ModulatorId);
            var extra = parameter.Bindings.Count > 1 ? " +" + (parameter.Bindings.Count - 1) : "";
            return "● " + (first?.Name ?? "?") + extra;
        }

        private void ToggleBinding(string name, PresetParameter parameter)
        {
            _editingParameter = _editingParameter == parameter ? null : parameter;
            if (_editingParameter == null || _pendingDepths.ContainsKey(parameter)) return;

            _pendingDepths[parameter] = DefaultDepth(name, parameter);
        }

        private static double DefaultDepth(string name, PresetParameter parameter)
        {
            if (name == "Opacity") return 1;
            if (name.StartsWith("Position")) return 100;
            if (name == "Rotation") return 90;

            return parameter.Base == 0 ? 1 : parameter.Base;
        }

        private string ParameterOutput(PresetParameter parameter)
        {
            try
            {
                var sum = parameter.Base + parameter.Bindings.Sum(binding => binding.Depth * DisplaySignal(binding.ModulatorId));
                var texture = _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null;
                var context = texture ? new ReferenceContext(texture.width, texture.height) : new ReferenceContext(1920, 1080);
                var reference = new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);
                return (reference.Evaluate(context) * sum).ToString("F2");
            }
            catch (ArgumentException)
            {
                return "—";
            }
        }

        private Element BindDetails(string name, PresetParameter parameter)
        {
            return UI.Column(
                UI.DynamicElementOnStatusChanged(
                    () => (BindingRevision(parameter), _modulatorsRevision, _modulatorNamesRevision),
                    _ => BuildBindings(parameter)),
                UI.DynamicElementOnStatusChanged(
                    () => (_modulatorsRevision, _modulatorNamesRevision),
                    _ => BuildBindingControls(name, parameter)),
                UI.Row(
                    Number("Multiplier", () => parameter.Multiplier, value => parameter.Multiplier = value),
                    Number("Absolute", () => parameter.AbsoluteValue, value => parameter.AbsoluteValue = value)
                ).Styled(style => style.marginTop = 6),
                T.Text("Reference × (Base + Σ Depth × Signal). Opacity uses Depth 1; size Depth follows the Reference.", 10, T.Muted)
                    .Styled(style =>
                    {
                        style.marginTop = 6;
                        style.whiteSpace = WhiteSpace.Normal;
                    })
            ).Styled(style =>
            {
                T.Border(style, T.BindLine, 0, 0, 0, 2);
                style.paddingLeft = 9;
                style.paddingTop = 4;
                style.paddingBottom = 4;
                style.marginTop = 8;
                style.marginBottom = 1;
            });
        }

        private Element BuildBindings(PresetParameter parameter)
        {
            var models = Document.Modulators.ToArray();
            return UI.Column(parameter.Bindings.Select(binding => (Element)UI.Row(
                T.Labeled("Value", Choice(
                    null,
                    () => Array.FindIndex(models, model => model.Id == binding.ModulatorId),
                    value =>
                    {
                        if (value < 0 || value >= models.Length) return;

                        binding.ModulatorId = models[value].Id;
                        Changed();
                    },
                    SignalNames(models))).SetFlexGrow(1).SetFlexShrink(1),
                T.Labeled("Depth", NumberInput(() => binding.Depth, value => binding.Depth = value))
                    .SetWidth(72).SetFlexShrink(0),
                T.Button("Remove", () =>
                {
                    parameter.Bindings.Remove(binding);
                    BindingStructureChanged(parameter);
                }).Styled(SmallButton)
            ).Styled(style =>
            {
                style.alignItems = Align.FlexEnd;
                style.marginBottom = 6;
            })).ToArray());
        }

        private Element BuildBindingControls(string name, PresetParameter parameter)
        {
            var models = Document.Modulators.ToArray();

            string SelectedId()
            {
                if (_pendingSignals.TryGetValue(parameter, out var id) && models.Any(model => model.Id == id)) return id;

                return models.FirstOrDefault(model => model.Kind == ModulatorKind.Adsr)?.Id ?? models.FirstOrDefault()?.Id;
            }

            return UI.Column(
                UI.Row(
                    T.Labeled("Add Value", Choice(
                        null,
                        () => Array.FindIndex(models, model => model.Id == SelectedId()),
                        index =>
                        {
                            if (index >= 0 && index < models.Length) _pendingSignals[parameter] = models[index].Id;
                        },
                        SignalNames(models)).SetInteractable(models.Length > 0)).SetFlexGrow(1).SetFlexShrink(1),
                    T.Labeled("Depth", UI.Field(
                        (LabelElement)null,
                        () => (float)_pendingDepths[parameter],
                        value => _pendingDepths[parameter] = value)).SetWidth(72).SetFlexShrink(0),
                    EnabledButton(
                        "+ Bind",
                        () => BindParameter(name, parameter, SelectedId(), false),
                        () => SelectedId() != null).Styled(SmallButton)
                ).Styled(style => style.alignItems = Align.FlexEnd),
                UI.Row(
                    EnabledButton(
                        "Bind ADSR from 0",
                        () => BindParameter(name, parameter, SelectedId(), true),
                        () => models.Any(model => model.Id == SelectedId() && model.Kind == ModulatorKind.Adsr))
                        .Styled(SmallButton),
                    T.Button("New ADSR", () => _pendingSignals[parameter] = CreateModulator(ModulatorKind.Adsr).Id)
                        .Styled(SmallButton),
                    UI.Space().SetFlexGrow(1),
                    T.Button("Close", () => _editingParameter = null).Styled(SmallButton)
                ).Styled(style => style.marginTop = 6));
        }

        private void BindParameter(string name, PresetParameter parameter, string modulatorId, bool fromZero)
        {
            Guard(() =>
            {
                var model = Document.Modulators.FirstOrDefault(entry => entry.Id == modulatorId)
                    ?? throw new ArgumentException("Choose a Modulator first.");
                var depth = _pendingDepths[parameter];
                if (double.IsNaN(depth) || double.IsInfinity(depth)) throw new ArgumentException("Depth must be finite.");
                if (fromZero && model.Kind != ModulatorKind.Adsr)
                    throw new ArgumentException("Choose an ADSR for an envelope from zero.");

                var binding = parameter.Bindings.FirstOrDefault(entry => entry.ModulatorId == modulatorId);
                if (binding == null) parameter.Bindings.Add(new PresetBinding { ModulatorId = modulatorId, Depth = depth });
                else binding.Depth = depth;

                if (fromZero) parameter.Base = 0;

                BindingStructureChanged(parameter);
                _status = $"Bound {model.Name} to {name}: Base {parameter.Base:G}, Depth {depth:G}. " +
                          "Use Beat Scrub or Animated Preview to see it move.";
            });
        }

        private static string[] SignalNames(ModulatorSettings[] models)
        {
            return models
                .Select((model, index) => $"{index + 1}. {model.Name} · {(model.Kind == ModulatorKind.Adsr ? "ADSR" : "LFO")}")
                .ToArray();
        }
    }
}
