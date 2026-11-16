using System.Collections.Generic;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Unity.Fonts;
using Krab.TypeLayout.Unity.Rendering;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.TypeLayout.Performance
{
    /// <summary>
    /// Overlay text for performances: switchable layers drawn on the output camera, edited from the settings UI
    /// and moving with the BPM clock.
    /// </summary>
    [RequireComponent(typeof(TypeLayoutCanvas), typeof(TypeLayoutFont))]
    [UiTarget("TypeLayout/Performance", "Type Layout", 0)]
    public sealed class TypeLayoutPerformance : MonoBehaviour, IUiTarget
    {
        // Used only when no tempo clock is injected, so scrolling still moves in a bare Scene.
        private const double FallbackBpm = 120;

        private static readonly Color SafeAreaGuideColor = new(0.2f, 0.9f, 1f, 0.5f);

        [SerializeField] private Camera _outputCamera;

        private readonly PrefsBool _visible = new("TypeLayout Visible", true);
        private readonly PrefsBool _showGuides = new("TypeLayout Guides", false);
        private readonly PrefsColor _color = new("TypeLayout Color", Color.white);
        private readonly PrefsFloat _margin = new("TypeLayout Margin", 40);

        private readonly TextLayer[] _layers =
        {
            new FrameTextLayer(),
            new LineTextLayer("Line A", false, "Gigandect", false, 0.25f),
            new LineTextLayer("Line B", false, "Gigandect", true, 0.75f),
            new CircleTextLayer(),
            new GridTextLayer()
        };

        private TypeLayoutCanvas _canvas;
        private TypeLayoutDrawer _drawer;
        private IBPMManager _bpmManager;

        private void Start()
        {
            var font = GetComponent<TypeLayoutFont>();
            if (!_outputCamera || font.Face == null)
            {
                Debug.LogError($"{nameof(TypeLayoutPerformance)} requires an output {nameof(Camera)} and a loaded font.",
                    this);
                enabled = false;
                return;
            }

            _canvas = GetComponent<TypeLayoutCanvas>();
            _canvas.Initialize(_outputCamera);
            _drawer = new TypeLayoutDrawer(_canvas, font.Face);
        }

        private void Update()
        {
            if (!_visible.Get()) return;

            var frame = new LayoutFrame(_outputCamera.pixelWidth, _outputCamera.pixelHeight);
            var margin = Mathf.Clamp(_margin.Get(), 0, (float)(frame.ReferenceHeight / 2 - 1));
            var safeArea = frame.SafeArea(margin);
            var beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            _drawer.ShowGuides = _showGuides.Get();
            if (_drawer.ShowGuides) UniteGuideRenderer.DrawRect(_canvas, safeArea, SafeAreaGuideColor);

            var context = new LayerContext(frame, safeArea, beats, _drawer, _color.Get());
            foreach (var layer in _layers)
                layer.Draw(context);
        }

        public bool Visible
        {
            get => _visible.Get();
            set => _visible.Set(value);
        }

        public IReadOnlyList<TextLayer> Layers => _layers;

        public Element CreateElement()
        {
            var layers = new Element[_layers.Length];
            for (var i = 0; i < _layers.Length; i++)
                layers[i] = _layers[i].CreateElement();

            return UI.Column(
                _visible.CreateElement("Visible"),
                _showGuides.CreateElement("Guides"),
                _color.CreateElement("Color"),
                _margin.CreateSlider("Margin", 0f, 300f),
                UI.Column(layers));
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }
    }
}
