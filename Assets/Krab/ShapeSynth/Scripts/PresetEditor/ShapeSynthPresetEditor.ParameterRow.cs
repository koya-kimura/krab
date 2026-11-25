using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>
    /// Parameters as table rows: Name | Base | Rand | Depth | Unit | Now | Flip | Bind.
    /// Rand is a ±random offset to Base per copy and trigger; Flip reverses the bound motion on some copies.
    /// Base and Depth are shown in the chosen unit; Now is the output at the preview Beat.
    /// </summary>
    public sealed partial class ShapeSynthPresetEditor
    {
        private const float NameColumnWidth = 84;
        private const float NumberColumnWidth = 58;
        private const float UnitColumnWidth = 66;
        private const float NowColumnWidth = 74;
        private const float BindColumnWidth = 96;
        private const float FlipColumnWidth = 76;

        private static readonly string[] FlipNames = Enum.GetNames(typeof(PresetFlip));
        private static readonly string[] AnchorNames = Enum.GetNames(typeof(PresetAnchor));
        private const float StepsFieldWidth = 140;

        /// <summary>
        /// Column headings, shown once above a block of Parameter rows. The header and every row are rebuilt
        /// with or without the Flip column, so no empty placeholder shifts the columns. RosettaUI's switching
        /// wrappers size to their content, so every switching cell also gets its column width explicitly.
        /// </summary>
        private static Element ParameterHeader(PresetLayout copies = null)
        {
            return UI.DynamicElementOnStatusChanged(() => HasCopies(copies), withFlip =>
            {
                Element Heading(string text, float width) =>
                    UI.Label(text).SetWidth(width).SetColor(PresetEditorPalette.Title);

                var cells = new List<Element>
                {
                    UI.Space().SetWidth(NameColumnWidth),
                    Heading("Base", NumberColumnWidth),
                    Heading("Rand", NumberColumnWidth),
                    Heading("Depth", NumberColumnWidth),
                    Heading("Unit", UnitColumnWidth),
                    Heading("Now", NowColumnWidth)
                };
                if (withFlip) cells.Add(Heading("Flip", FlipColumnWidth));
                cells.Add(Heading("Bind", BindColumnWidth));
                return UI.Row(cells.ToArray());
            });
        }

        private static bool HasCopies(PresetLayout copies) => copies != null && copies.Kind != PresetLayoutKind.None;

        /// <param name="copies">The owning Shape's Layout; Flip is offered only while it has copies.</param>
        /// <param name="initialDepth">The first Depth when a Value is bound; independent of the label.</param>
        private Element ParameterRow(string name, PresetParameter parameter, PresetQuantity quantity,
            PresetLayout copies = null, InitialDepth initialDepth = InitialDepth.FromBase)
        {
            return UI.DynamicElementOnStatusChanged(() => HasCopies(copies),
                withFlip => ParameterCells(name, parameter, quantity, copies, initialDepth, withFlip));
        }

        private Element ParameterCells(string name, PresetParameter parameter, PresetQuantity quantity,
            PresetLayout copies, InitialDepth initialDepth, bool withFlip)
        {
            var label = UI.Label(name).SetWidth(NameColumnWidth);
            label.RegisterUpdateCallback(element =>
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);

            var cells = new List<Element>
            {
                label,
                NumberCell(() => parameter.Base, value => parameter.Base = value).SetWidth(NumberColumnWidth),
                NumberCell(() => parameter.Random, value => parameter.Random = Math.Abs(value))
                    .SetWidth(NumberColumnWidth),
                UI.DynamicElementOnStatusChanged(
                    () => parameter.GetBinding() != null,
                    bound => bound
                        ? DepthField(parameter).SetWidth(NumberColumnWidth)
                        : UI.Label("-").SetWidth(NumberColumnWidth).SetColor(PresetEditorPalette.DragSource))
                    .SetWidth(NumberColumnWidth),
                UnitColumn(parameter, quantity).SetWidth(UnitColumnWidth),
                UI.Label(() => NowText(parameter, quantity, copies)).SetWidth(NowColumnWidth)
            };
            if (withFlip)
            {
                cells.Add(UI.DynamicElementOnStatusChanged(
                        () => parameter.GetBinding() != null,
                        bound => bound
                            ? Choice(null, () => (int)parameter.Flip, value =>
                            {
                                parameter.Flip = (PresetFlip)value;
                                Changed();
                            }, FlipNames).SetWidth(FlipColumnWidth)
                            : UI.Label("-").SetWidth(FlipColumnWidth).SetColor(PresetEditorPalette.DragSource))
                    .SetWidth(FlipColumnWidth));
            }

            cells.Add(BindButton(parameter, initialDepth, name).SetWidth(BindColumnWidth));
            return UI.Column(UI.Row(cells.ToArray()), RandomStepsLine(parameter));
        }

        /// <summary>
        /// Shown under the row only while Rand is set: Steps 0 keeps any value in ±Rand, 2 or more picks one of that
        /// many evenly spaced values (5 → one of five fixed positions). Placed under the Rand column.
        /// </summary>
        private Element RandomStepsLine(PresetParameter parameter)
        {
            return UI.DynamicElementIf(() => parameter.Random != 0, () => UI.Row(
                UI.Space().SetWidth(NameColumnWidth + NumberColumnWidth),
                UI.Field("Steps", () => parameter.RandomSteps, value =>
                {
                    parameter.RandomSteps = Math.Clamp(value, 0, RandomRange.MaxSteps);
                    Changed();
                }).SetWidth(StepsFieldWidth)));
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

        /// <summary>
        /// Output of Base + Depth × signal at the preview Beat. It is shown while bound, for screen units, or
        /// with Rand. A leading "~" means copies differ from it (Rand, Flip or Delay are not applied here).
        /// </summary>
        private string NowText(PresetParameter parameter, PresetQuantity quantity, PresetLayout copies)
        {
            var binding = parameter.GetBinding();
            var screen = PresetUnits.IsScreenRelative(PresetUnits.UnitOf(parameter, quantity));
            if (binding == null && !screen && parameter.Random == 0) return "";

            var value = binding == null ? parameter.Base : parameter.Base + binding.Depth * SignalAtBeat(binding.ValueId);
            var varies = parameter.Random != 0 || (binding != null && copies != null
                && ((copies.Kind != PresetLayoutKind.None && parameter.Flip != PresetFlip.None) || copies.Delay > 0));
            return (varies ? "~" : "") + OutputText(parameter, quantity, value);
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
