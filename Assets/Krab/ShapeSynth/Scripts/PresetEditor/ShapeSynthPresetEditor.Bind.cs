using System;
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
        /// The row's Bind column: the bound Value's name, or None. Pressing it opens the Bind picker for this
        /// Parameter, where the Values are shown as graphs.
        /// </summary>
        private Element BindButton(PresetParameter parameter, InitialDepth initialDepth, string parameterName)
        {
            ButtonElement button = null;
            button = UI.Button(
                UI.Label(() => FindValue(parameter.GetBinding()?.ValueId) is { } value ? DisplayName(value) : NoValueName),
                () => OpenBindPicker(button, parameter, initialDepth, $"{Selected.Name} / {parameterName}"));
            button.RegisterUpdateCallback(element =>
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null);
            return button;
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
