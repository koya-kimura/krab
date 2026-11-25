using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const string NoValueName = "None";

        /// <summary>How a new Binding picks its first Depth, so the motion is visible without overshooting.</summary>
        private enum InitialDepth
        {
            /// <summary>The Parameter's Base, or 1 when Base is 0.</summary>
            FromBase,
            One,
            Pixels100,
            Degrees90,
            Degrees360
        }

        /// <summary>
        /// The row's Bind column: a dropdown of None and every Value as "group/name", in group order. Rebuilt when
        /// Values or their names change.
        /// </summary>
        private Element BindChoice(PresetParameter parameter, InitialDepth initialDepth)
        {
            return UI.DynamicElementOnStatusChanged(
                () => (_valuesRevision, _valueNamesRevision),
                _ =>
                {
                    var values = ValuesInGroupOrder();
                    var choice = Choice(
                        null,
                        () => Array.FindIndex(values, value => value.Id == parameter.GetBinding()?.ValueId) + 1,
                        index => BindValue(parameter, initialDepth,
                            index == 0 ? null : values.ElementAtOrDefault(index - 1)),
                        new[] { NoValueName }.Concat(values.Select(value => value.Name)));
                    choice.RegisterUpdateCallback(element =>
                        element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);
                    return choice.SetWidth(BindColumnWidth);
                });
        }

        private void BindValue(PresetParameter parameter, InitialDepth initialDepth, PresetValue value)
        {
            if (value == null)
            {
                if (parameter.GetBinding() == null) return;

                parameter.Unbind();
                Changed();
                return;
            }

            var current = parameter.GetBinding();
            if (current?.ValueId == value.Id) return;

            parameter.Bind(value.Id, current?.Depth ?? DefaultDepth(initialDepth, parameter, OutputContext()));
            Changed();
        }

        /// <summary>
        /// A visible first Depth. Pixel and degree policies aim at a fixed output converted into the Parameter's
        /// unit, so screen units and turns do not overshoot.
        /// </summary>
        private static double DefaultDepth(InitialDepth initialDepth, PresetParameter parameter, ReferenceContext context)
        {
            return initialDepth switch
            {
                InitialDepth.One => 1,
                InitialDepth.Pixels100 => InUnit(parameter, 100, context),
                InitialDepth.Degrees90 => InUnit(parameter, 90, context),
                InitialDepth.Degrees360 => InUnit(parameter, 360, context),
                _ => parameter.Base == 0 ? 1 : parameter.Base
            };
        }

        /// <summary>The stored value whose output is the given pixels/degrees; the output itself if invalid.</summary>
        private static double InUnit(PresetParameter parameter, double output, ReferenceContext context)
        {
            try
            {
                var size = new ReferenceDefinition(parameter.Reference, parameter.Multiplier, parameter.AbsoluteValue)
                    .Evaluate(context);
                return size == 0 ? output : output / size;
            }
            catch (ArgumentException)
            {
                return output;
            }
        }
    }
}
