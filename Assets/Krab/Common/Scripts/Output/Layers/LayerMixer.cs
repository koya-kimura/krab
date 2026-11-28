using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.Common.Output.Layers
{
    /// <summary>
    /// Shows, hides and reorders the drawing layers stacked on the output camera from one settings page.
    /// Each layer is an <see cref="OverlayLayerCamera"/>; the saved order is a list of layer names, front first.
    /// </summary>
    [UiTarget("Layers", "Layers", 0)]
    public sealed class LayerMixer : MonoBehaviour, IUiTarget
    {
        private const int OrderStep = 10;
        private const char NameSeparator = ',';

        [Tooltip("Front to back. Used until an order is saved; layers missing from the saved order go to the back.")]
        [SerializeField] private Layer[] _layers = Array.Empty<Layer>();

        private readonly PrefsString _order = new("Layers Order", string.Empty);
        private readonly List<Layer> _frontToBack = new();

        private string _appliedOrder;

        private void Start()
        {
            foreach (var layer in _layers)
            {
                if (!layer.Camera)
                    Debug.LogError($"{nameof(LayerMixer)} requires an {nameof(OverlayLayerCamera)} for {layer.Name}.",
                        this);
            }
        }

        // Polled so values loaded or reset through PrefsGUI apply the same way as edits from the page.
        private void Update()
        {
            var order = _order.Get();
            if (order != _appliedOrder) ApplyOrder(order);

            foreach (var layer in _layers)
                layer.ApplyVisibility();
        }

        public Element CreateElement()
        {
            return UI.Column(
                UI.Label("Top draws in front."),
                UI.DynamicElementOnStatusChanged(() => _appliedOrder, _ => UI.Column(
                    _frontToBack.Select(CreateRow).ToArray())));
        }

        private void ApplyOrder(string order)
        {
            _frontToBack.Clear();
            foreach (var name in order.Split(NameSeparator))
            {
                var layer = Array.Find(_layers, candidate => candidate.Name == name.Trim());
                if (layer != null && !_frontToBack.Contains(layer)) _frontToBack.Add(layer);
            }

            foreach (var layer in _layers)
            {
                if (!_frontToBack.Contains(layer)) _frontToBack.Add(layer);
            }

            for (int index = 0; index < _frontToBack.Count; index++)
            {
                var camera = _frontToBack[index].Camera;
                if (camera) camera.Order = (_frontToBack.Count - 1 - index) * OrderStep;
            }

            _appliedOrder = order;
        }

        private Element CreateRow(Layer layer)
        {
            return UI.Row(
                UI.Button("Up", () => Move(layer, -1)),
                UI.Button("Down", () => Move(layer, 1)),
                layer.CreateVisibleElement());
        }

        private void Move(Layer layer, int offset)
        {
            int index = _frontToBack.IndexOf(layer);
            int target = index + offset;
            if (index < 0 || target < 0 || target >= _frontToBack.Count) return;

            var names = _frontToBack.Select(item => item.Name).ToList();
            (names[index], names[target]) = (names[target], names[index]);
            var order = string.Join(NameSeparator, names);
            _order.Set(order);
            ApplyOrder(order);
        }

        [Serializable]
        private sealed class Layer
        {
            [Tooltip("Shown on the page and used as the saved key; renaming it drops the saved settings.")]
            [SerializeField] private string _name;
            [SerializeField] private OverlayLayerCamera _camera;

            private PrefsBool _visible;

            public string Name => _name;

            public OverlayLayerCamera Camera => _camera;

            private PrefsBool Visible => _visible ??= new PrefsBool($"Layers {_name} Visible", true);

            public void ApplyVisibility()
            {
                if (!_camera) return;

                var visible = Visible.Get();
                if (_camera.enabled != visible) _camera.enabled = visible;
            }

            public Element CreateVisibleElement()
            {
                return Visible.CreateElement(_name);
            }
        }
    }
}
