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

        // RosettaUI's own window-launcher icon ("open-in-new"), styled by its default USS.
        private const string WindowIconClass = "rosettaui-window-launcher__checkmark";
        private static readonly Color WindowIconTint = new(0.66f, 0.66f, 0.66f);

        private Element _inspectorPane;
        private WindowElement _bindWindow;
        private PresetParameter _bindParameter;
        private string _bindName;
        private PresetQuantity _bindQuantity;

        /// <summary>Icon-only button that opens the Parameter's Bind window.</summary>
        private Element ParameterWindowButton(string name, PresetParameter parameter, PresetQuantity quantity)
        {
            ButtonElement button = null;
            button = UI.Button("", () => OpenBind(name, parameter, quantity, button));
            button.RegisterUpdateCallback(element =>
            {
                var editing = _bindParameter == parameter && _bindWindow is { IsOpen: true };
                element.Style.BackgroundColor = editing ? PresetEditorPalette.Selected : null;

                var icon = WindowIcon(element);
                if (icon != null)
                {
                    icon.style.unityBackgroundImageTintColor =
                        parameter.GetBinding() != null ? PresetEditorPalette.Bound : WindowIconTint;
                }
            });
            return button.SetWidth(28);
        }

        private static UnityEngine.UIElements.VisualElement WindowIcon(Element button)
        {
            var view = UIToolkitBuilder.Instance.GetUIObj(button);
            if (view == null) return null;

            var icon = UnityEngine.UIElements.UQueryExtensions.Q(view, className: WindowIconClass);
            if (icon != null) return icon;

            icon = new UnityEngine.UIElements.VisualElement();
            icon.AddToClassList(WindowIconClass);
            view.Add(icon);
            return icon;
        }

        /// <summary>Opens the Bind window left of the Inspector, level with the button that asked for it.</summary>
        private void OpenBind(string name, PresetParameter parameter, PresetQuantity quantity, Element button)
        {
            _bindName = name;
            _bindParameter = parameter;
            _bindQuantity = quantity;
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

        /// <summary>Value choice, Depth in the row's unit, and the output range the Value sweeps.</summary>
        private Element BuildBindFields()
        {
            var parameter = _bindParameter;
            var name = _bindName;
            var quantity = _bindQuantity;
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
                    () => UI.Column(
                        UI.Row(
                            UI.Label("Depth").SetWidth(NameColumnWidth),
                            DepthField(parameter, quantity).SetWidth(NumberColumnWidth),
                            UI.Label(() => PresetUnits.Symbol(PresetUnits.UnitOf(parameter, quantity)))),
                        UI.Label(() => "Range  " + RangeText(parameter, quantity)),
                        UI.Label(() => "Now  " + NowText(parameter, quantity)))),
                UI.Row(
                    UI.Button("Unbind", () => BindValue(name, parameter, null))
                        .RegisterUpdateCallback(element => element.Interactable = parameter.GetBinding() != null),
                    UI.Button("Close", CloseBind))
            ).SetWidth(BindWindowWidth);
        }

        /// <summary>Output at the lowest and highest Value sampled over the graph range (ADSR 0–1, LFO may be ±1).</summary>
        private string RangeText(PresetParameter parameter, PresetQuantity quantity)
        {
            var binding = parameter.GetBinding();
            var value = binding == null ? null : FindValue(binding.ValueId);
            if (value == null) return "-";

            const int samples = 96;
            double low = double.MaxValue, high = double.MinValue;
            for (var i = 0; i <= samples; i++)
            {
                var signal = PresetSignalSampler.SampleOrZero(value, PresetSignalSampler.GraphBeats * i / samples);
                low = System.Math.Min(low, signal);
                high = System.Math.Max(high, signal);
            }

            var from = OutputText(parameter, quantity, parameter.Base + binding.Depth * low);
            var to = OutputText(parameter, quantity, parameter.Base + binding.Depth * high);
            return from + " - " + to;
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
