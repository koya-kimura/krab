using System;
using System.Linq;
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

            parameter.Bind(value.Id, current?.Depth ?? DefaultDepth(name, parameter));
            Changed();
        }

        /// <summary>A visible first Depth in stored units: Turn counts in turns, other angles in degrees.</summary>
        private static double DefaultDepth(string name, PresetParameter parameter)
        {
            var turn = PresetUnits.UnitOf(parameter, PresetQuantity.Angle) == PresetUnit.Turn;
            if (name == "Opacity") return 1;
            if (name.StartsWith("Position")) return 100;
            if (name == "Rotation") return turn ? 0.25 : 90;
            if (name == "Hue Shift") return turn ? 1 : 360;

            return parameter.Base == 0 ? 1 : parameter.Base;
        }
    }
}
