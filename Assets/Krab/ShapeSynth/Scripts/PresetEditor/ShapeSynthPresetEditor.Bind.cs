using System.Linq;
using Krab.ShapeSynth.Core.Presets;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;

namespace Krab.ShapeSynth.PresetEditor
{
    public sealed partial class ShapeSynthPresetEditor
    {
        private const string NoValueName = "None";
        private const float BindWindowWidth = 300;

        private Element _inspectorPane;
        private WindowElement _bindWindow;
        private PresetParameter _bindParameter;
        private string _bindName;

        private Element BindButton(string name, PresetParameter parameter)
        {
            ButtonElement button = null;
            button = UI.Button(UI.Label(() => FindValue(parameter.GetBinding()?.ValueId)?.Name ?? "Bind"), () =>
                OpenBind(name, parameter, button));
            button.RegisterUpdateCallback(element =>
            {
                var editing = _bindParameter == parameter && _bindWindow is { IsOpen: true };
                element.Style.BackgroundColor = editing ? PresetEditorPalette.Selected : null;
                element.Style.Color = parameter.GetBinding() != null ? PresetEditorPalette.Bound : null;
            });
            return button.SetMinWidth(90);
        }

        /// <summary>Opens the Bind window left of the Inspector, level with the button that asked for it.</summary>
        private void OpenBind(string name, PresetParameter parameter, Element button)
        {
            _bindName = name;
            _bindParameter = parameter;
            _bindWindow ??= UI.Window(
                UI.Label(() => _bindName),
                UI.DynamicElementOnStatusChanged(
                    () => (_bindParameter, _valuesRevision, _valueNamesRevision),
                    _ => BuildBindFields()));

            var buttonView = UIToolkitBuilder.Instance.GetUIObj(button);
            var paneView = UIToolkitBuilder.Instance.GetUIObj(_inspectorPane);
            if (buttonView != null && paneView != null)
            {
                var left = paneView.worldBound.xMin - BindWindowWidth - 16;
                _bindWindow.Position = new Vector2(Mathf.Max(0, left), buttonView.worldBound.yMin);
            }

            if (!_bindWindow.HasBuilt)
            {
                var root = FindFirstObjectByType<RosettaUIRoot>();
                if (!root) return;

                root.Build(_bindWindow);
            }

            _bindWindow.Open();
        }

        private void CloseBind()
        {
            _bindParameter = null;
            _bindWindow?.Close();
        }

        private Element BuildBindFields()
        {
            var parameter = _bindParameter;
            var name = _bindName;
            if (parameter == null) return UI.Space();

            var values = Document.Values.ToArray();
            return UI.Column(
                Choice(
                    "Value",
                    () => System.Array.FindIndex(values, value => value.Id == parameter.GetBinding()?.ValueId) + 1,
                    index => BindValue(name, parameter, index == 0 ? null : values.ElementAtOrDefault(index - 1)),
                    new[] { NoValueName }.Concat(values.Select(value => value.Name))),
                UI.DynamicElementIf(
                    () => parameter.GetBinding() != null,
                    () => NumberField("Depth", () => parameter.GetBinding()?.Depth ?? 0, depth =>
                    {
                        var binding = parameter.GetBinding();
                        if (binding != null) binding.Depth = depth;
                    })),
                NumberField("Base", () => parameter.Base, value => parameter.Base = value),
                UI.Label(() => "Output  " + ParameterValueText(parameter)),
                UI.Row(
                    UI.Button("Unbind", () => BindValue(name, parameter, null))
                        .RegisterUpdateCallback(element => element.Interactable = parameter.GetBinding() != null),
                    UI.Button("Close", CloseBind))
            ).SetWidth(BindWindowWidth);
        }

        private string ParameterValueText(PresetParameter parameter)
        {
            var binding = parameter.GetBinding();
            var value = binding == null ? parameter.Base : parameter.Base + binding.Depth * SignalAtBeat(binding.ValueId);
            return ParameterOutput(parameter, value);
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

        private static double DefaultDepth(string name, PresetParameter parameter)
        {
            if (name == "Opacity") return 1;
            if (name.StartsWith("Position")) return 100;
            if (name == "Rotation") return 90;

            return parameter.Base == 0 ? 1 : parameter.Base;
        }
    }
}
