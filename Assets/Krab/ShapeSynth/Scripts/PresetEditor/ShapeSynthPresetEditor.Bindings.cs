using System;
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
        private const string NoValueName = "None · stays at Base";

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
                    BindButton(parameter).SetWidth(96).SetFlexShrink(0)
                ).Styled(style => style.alignItems = Align.FlexEnd),
                UI.DynamicElementIf(() => _editingParameter == parameter, () => BindDetails(name, parameter))
            ).Styled(style =>
            {
                T.Padding(style, 8, 0);
                T.Border(style, T.Hex(0x303640), 0, 0, 1, 0);
            });
        }

        private Element BindButton(PresetParameter parameter)
        {
            // Green = bound, blue = its editor is open, grey = unbound; hover/press come from PresetButtonStyle.
            var button = PresetButtonStyle.Create(
                T.Text(() => BindLabel(parameter), 10),
                () => _editingParameter = _editingParameter == parameter ? null : parameter,
                primary: () => _editingParameter == parameter && parameter.GetBinding() == null,
                active: () => parameter.GetBinding() != null);
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
            var model = BoundModel(parameter);
            if (model != null) return "● " + model.Name;

            return _editingParameter == parameter ? "▾ Bind" : "+ Bind";
        }

        /// <summary>
        /// One Value per Parameter: pick it, set Depth, and read the resulting output range. Edits apply at once.
        /// </summary>
        private Element BindDetails(string name, PresetParameter parameter)
        {
            return UI.Column(
                T.Text("VALUE", 10, T.Muted).Styled(style => style.marginBottom = 2),
                UI.DynamicElementOnStatusChanged(
                    () => (_modulatorsRevision, _modulatorNamesRevision),
                    _ => BuildValuePicker(name, parameter)),
                UI.DynamicElementIf(
                    () => parameter.GetBinding() != null,
                    () => BuildBoundControls(name, parameter)),
                UI.DynamicElementIf(
                    () => parameter.GetBinding() == null,
                    () => T.Text("Choose a Value to animate this Parameter.", 10, T.Muted)
                        .Styled(style => style.marginTop = 6)),
                UI.Row(
                    Number("Ref. multiplier", () => parameter.Multiplier, value => parameter.Multiplier = value),
                    Number("Absolute value", () => parameter.AbsoluteValue, value => parameter.AbsoluteValue = value)
                ).Styled(style => style.marginTop = 10),
                UI.Row(
                    T.Text(() => FormulaText(parameter), 10, T.Muted).SetFlexGrow(1).Styled(style =>
                    {
                        style.whiteSpace = WhiteSpace.Normal;
                        style.marginRight = 6;
                    }),
                    T.Button("Done", () => _editingParameter = null).Styled(SmallButton)
                ).Styled(style =>
                {
                    style.alignItems = Align.Center;
                    style.marginTop = 6;
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

        private Element BuildValuePicker(string name, PresetParameter parameter)
        {
            var models = Document.Modulators.ToArray();
            var options = new[] { NoValueName }.Concat(SignalNames(models));
            return UI.Row(
                Choice(
                    null,
                    () => Array.FindIndex(models, model => model.Id == parameter.GetBinding()?.ModulatorId) + 1,
                    index => SelectValue(name, parameter, index == 0 ? null : models.ElementAtOrDefault(index - 1)),
                    options).SetFlexGrow(1).SetFlexShrink(1),
                T.Button("+ ADSR", () => BindNewValue(name, parameter, ModulatorKind.Adsr)).Styled(SmallButton),
                T.Button("+ LFO", () => BindNewValue(name, parameter, ModulatorKind.Lfo)).Styled(SmallButton)
            ).Styled(style => style.alignItems = Align.Center);
        }

        private Element BuildBoundControls(string name, PresetParameter parameter)
        {
            return UI.Column(
                UI.Row(
                    T.Labeled("Depth", NumberInput(
                        () => parameter.GetBinding()?.Depth ?? 0,
                        value =>
                        {
                            var binding = parameter.GetBinding();
                            if (binding != null) binding.Depth = value;
                        })).SetWidth(90).SetFlexShrink(0),
                    T.Labeled("Output range", T.Text(() => RangeText(parameter), 11, T.Output))
                        .SetFlexGrow(1).Styled(style => style.marginLeft = 10)
                ).Styled(style => style.alignItems = Align.FlexEnd),
                UI.Row(
                    EnabledButton("Start from 0 (Base = 0)", () => StartFromZero(name, parameter),
                        () => parameter.Base != 0).Styled(SmallButton),
                    UI.Space().SetFlexGrow(1),
                    T.Button("Unbind", () => SelectValue(name, parameter, null)).Styled(SmallButton)
                ).Styled(style => style.marginTop = 6)
            ).Styled(style => style.marginTop = 6);
        }

        private void SelectValue(string name, PresetParameter parameter, ModulatorSettings model)
        {
            Guard(() =>
            {
                if (model == null)
                {
                    if (parameter.GetBinding() == null) return;

                    parameter.Unbind();
                    Changed();
                    _status = $"Unbound {name}. It stays at Base {parameter.Base:G}.";
                    return;
                }

                var current = parameter.GetBinding();
                if (current?.ModulatorId == model.Id) return;

                parameter.Bind(model.Id, current?.Depth ?? DefaultDepth(name, parameter));
                Changed();
                _status = $"Bound {model.Name} to {name}. Use Beat Scrub or Preview to see it move.";
            });
        }

        private void BindNewValue(string name, PresetParameter parameter, ModulatorKind kind)
        {
            SelectValue(name, parameter, CreateModulator(kind));
        }

        private void StartFromZero(string name, PresetParameter parameter)
        {
            parameter.Base = 0;
            Changed();
            _status = $"{name} now starts from 0 and moves by Depth × Value.";
        }

        private ModulatorSettings BoundModel(PresetParameter parameter)
        {
            var id = parameter.GetBinding()?.ModulatorId;
            return id == null ? null : Document.Modulators.FirstOrDefault(model => model.Id == id);
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
            var binding = parameter.GetBinding();
            var signal = binding == null ? 0 : DisplaySignal(binding.ModulatorId);
            return FormatOutput(parameter, signal);
        }

        private string RangeText(PresetParameter parameter)
        {
            var model = BoundModel(parameter);
            if (model == null) return "—";

            // ADSR moves 0 → 1; LFO swings −1 → +1.
            var low = model.Kind == ModulatorKind.Adsr ? 0 : -1;
            var kind = model.Kind == ModulatorKind.Adsr ? "ADSR 0 → 1" : "LFO −1 → +1";
            return $"{FormatOutput(parameter, low)} → {FormatOutput(parameter, 1)}  ({kind})";
        }

        private static string FormulaText(PresetParameter parameter)
        {
            return parameter.GetBinding() == null
                ? "Output = Reference × Base"
                : "Output = Reference × (Base + Depth × Value)";
        }

        private string FormatOutput(PresetParameter parameter, double signal)
        {
            try
            {
                var sum = parameter.Base + (parameter.GetBinding()?.Depth ?? 0) * signal;
                var texture = _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null;
                var context = texture
                    ? new ReferenceContext(texture.width, texture.height)
                    : new ReferenceContext(1920, 1080);
                var reference = new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue);
                return (reference.Evaluate(context) * sum).ToString("F2");
            }
            catch (ArgumentException)
            {
                return "—";
            }
        }

        private static string[] SignalNames(ModulatorSettings[] models)
        {
            return models
                .Select((model, index) => $"{index + 1}. {model.Name} · {(model.Kind == ModulatorKind.Adsr ? "ADSR" : "LFO")}")
                .ToArray();
        }
    }
}
