using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Parameters as table rows: Name | Base | Depth | Unit | Now | window icon.
    /// Base and Depth are shown in the chosen unit; Now is the output at the preview Beat.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        private const float NameColumnWidth = 84;
        private const float NumberColumnWidth = 58;
        private const float UnitColumnWidth = 66;
        private const float NowColumnWidth = 74;

        /// <summary>Column headings, shown once above a block of Parameter rows.</summary>
        private static Element ParameterHeader()
        {
            Element Heading(string text, float width) =>
                UI.Label(text).SetWidth(width).SetColor(PresetEditorPalette.Title);

            return UI.Row(
                UI.Space().SetWidth(NameColumnWidth),
                Heading("Base", NumberColumnWidth),
                Heading("Depth", NumberColumnWidth),
                Heading("Unit", UnitColumnWidth),
                Heading("Now", NowColumnWidth));
        }

        private Element ParameterRow(string name, PresetParameter parameter, PresetQuantity quantity)
        {
            var label = UI.Label(name).SetWidth(NameColumnWidth);
            label.RegisterUpdateCallback(element =>
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);

            return UI.Row(
                label,
                NumberCell(() => parameter.Base, value => parameter.Base = value)
                    .SetWidth(NumberColumnWidth),
                UI.DynamicElementOnStatusChanged(
                    () => parameter.GetBinding() != null,
                    bound => bound
                        ? DepthField(parameter).SetWidth(NumberColumnWidth)
                        : UI.Label("-").SetWidth(NumberColumnWidth).SetColor(PresetEditorPalette.DragSource)),
                UnitColumn(parameter, quantity).SetWidth(UnitColumnWidth),
                UI.Label(() => NowText(parameter, quantity)).SetWidth(NowColumnWidth),
                ParameterWindowButton(name, parameter, quantity));
        }

        private Element DepthField(PresetParameter parameter)
        {
            return NumberCell(() => parameter.GetBinding()?.Depth ?? 0, value =>
            {
                var binding = parameter.GetBinding();
                if (binding != null) binding.Depth = value;
            });
        }

        /// <summary>A label-less number in the Parameter's unit; stored values are shown as they are.</summary>
        private Element NumberCell(Func<double> read, Action<double> write)
        {
            return UI.Field((LabelElement)null, () => (float)read(), value =>
            {
                write(value);
                Changed();
            });
        }

        /// <summary>A dropdown when the quantity has several units, otherwise the fixed unit as text.</summary>
        private Element UnitColumn(PresetParameter parameter, PresetQuantity quantity)
        {
            var units = PresetUnits.Choices(quantity);
            if (units.Count == 1) return UI.Label(PresetUnits.Symbol(units[0]));

            return Choice(
                null,
                () => Math.Max(0, units.ToList().IndexOf(PresetUnits.UnitOf(parameter, quantity))),
                index => Guard(() =>
                {
                    PresetUnits.SetUnit(parameter, quantity, units[index], OutputContext());
                    Changed();
                }),
                units.Select(PresetUnits.Symbol));
        }

        /// <summary>The output only adds information while bound or when a screen unit hides the pixels.</summary>
        private string NowText(PresetParameter parameter, PresetQuantity quantity)
        {
            var screen = PresetUnits.IsScreenRelative(PresetUnits.UnitOf(parameter, quantity));
            if (parameter.GetBinding() == null && !screen) return "";

            var binding = parameter.GetBinding();
            var value = binding == null ? parameter.Base : parameter.Base + binding.Depth * SignalAtBeat(binding.ValueId);
            return OutputText(parameter, quantity, value);
        }

        /// <summary>Reference × value as text with the output unit, or "-" for an invalid Reference.</summary>
        private string OutputText(PresetParameter parameter, PresetQuantity quantity, double value)
        {
            try
            {
                var reference = new ReferenceDefinition(parameter.Reference, parameter.Multiplier,
                    parameter.AbsoluteValue);
                return (reference.Evaluate(OutputContext()) * value).ToString("0.##")
                       + PresetUnits.OutputSymbol(quantity);
            }
            catch (ArgumentException)
            {
                return "-";
            }
        }

        private ReferenceContext OutputContext()
        {
            var texture = _controller && _controller.OutputCamera ? _controller.OutputCamera.targetTexture : null;
            return texture ? new ReferenceContext(texture.width, texture.height) : new ReferenceContext(1920, 1080);
        }
    }
}
