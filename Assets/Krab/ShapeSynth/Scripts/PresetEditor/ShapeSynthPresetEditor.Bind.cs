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

        /// <summary>
        /// The row's Bind column: a dropdown of None and every Value. Rebuilt when Values or their names change.
        /// </summary>
        private Element BindChoice(string name, PresetParameter parameter)
        {
            return UI.DynamicElementOnStatusChanged(
                () => (_valuesRevision, _valueNamesRevision),
                _ =>
                {
                    var values = Document.Values.ToArray();
                    var choice = Choice(
                        null,
                        () => Array.FindIndex(values, value => value.Id == parameter.GetBinding()?.ValueId) + 1,
                        index => BindValue(name, parameter, index == 0 ? null : values.ElementAtOrDefault(index - 1)),
                        new[] { NoValueName }.Concat(values.Select(value => value.Name)));
                    choice.RegisterUpdateCallback(element =>
                        element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);
                    return choice.SetWidth(BindColumnWidth);
                });
        }

        private void BindValue(string name, PresetParameter parameter, PresetValue value)
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

            parameter.Bind(value.Id, current?.Depth ?? DefaultDepth(name, parameter, OutputContext()));
            Changed();
        }

        /// <summary>
        /// A visible first Depth. Position, Rotation and Hue aim at a fixed output (100px, 90deg, 360deg)
        /// converted into the Parameter's unit, so screen units and turns do not overshoot.
        /// </summary>
        private static double DefaultDepth(string name, PresetParameter parameter, ReferenceContext context)
        {
            if (name == "Opacity") return 1;
            if (name.StartsWith("Position")) return InUnit(parameter, 100, context);
            if (name == "Rotation") return InUnit(parameter, 90, context);
            if (name == "Hue Shift") return InUnit(parameter, 360, context);

            return parameter.Base == 0 ? 1 : parameter.Base;
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
