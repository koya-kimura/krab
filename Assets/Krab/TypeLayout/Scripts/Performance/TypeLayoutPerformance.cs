using System;
using System.Collections.Generic;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Performance.Patterns;
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
    /// Overlay text for performances: script-written patterns drawn on the output camera, switched and tuned from
    /// the settings UI and moving with the BPM clock.
    /// </summary>
    [RequireComponent(typeof(TypeLayoutCanvas), typeof(TypeLayoutFont))]
    [UiTarget("TypeLayout/Performance", "Type Layout", 0)]
    public sealed class TypeLayoutPerformance : MonoBehaviour, IUiTarget
    {
        // Used only when no tempo clock is injected, so patterns still move in a bare Scene.
        private const float FallbackBpm = 120;

        private static readonly Color SafeAreaGuideColor = new(0.2f, 0.9f, 1f, 0.5f);

        [SerializeField] private Camera _outputCamera;

        private readonly PrefsBool _visible = new("TypeLayout Visible", true);
        private readonly PrefsBool _showGuides = new("TypeLayout Guides", false);
        private readonly PrefsColor _color = new("TypeLayout Color", Color.white);
        private readonly PrefsFloat _margin = new("TypeLayout Margin", 40);

        private readonly TypePattern[] _patterns =
        {
            new FrameLoopPattern(),
            new CornersPattern(),
            new TickerPattern(),
            new ColumnsPattern(),
            new CrossPattern(),
            new WavePattern(),
            new OrbitPattern(),
            new LetterGridPattern(),
            new SpiralPattern(),
            new DiagonalPattern(),
            new ZigzagPattern(),
            new BigLetterPattern(),
            new RadialPattern(),
            new DialPattern(),
            new ScatterPattern(),
            new TunnelPattern(),
            new FlickerPattern()
        };

        private readonly TypeWords _words = new();

        private TypeLayoutCanvas _canvas;
        private TypeLayoutDrawer _drawer;
        private IBPMManager _bpmManager;

        private void Start()
        {
            if (!TryInitialize()) enabled = false;
        }

        private void Update()
        {
            // A script reload during Play clears the drawer without calling Start again.
            if (_drawer == null && !TryInitialize()) return;
            if (!_visible.Get()) return;

            var frame = new LayoutFrame(_outputCamera.pixelWidth, _outputCamera.pixelHeight);
            var margin = Mathf.Clamp(_margin.Get(), 0, (float)(frame.ReferenceHeight / 2 - 1));
            var safeArea = frame.SafeArea(margin);
            var bpm = _bpmManager?.CurrentBpm ?? FallbackBpm;
            var beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            _drawer.ShowGuides = _showGuides.Get();
            if (_drawer.ShowGuides) UniteGuideRenderer.DrawRect(_canvas, safeArea, SafeAreaGuideColor);

            _words.BeginFrame(beats, bpm, DateTime.Now);
            var context = new PatternContext(frame, safeArea, beats, bpm, _drawer, _color.Get(), _words);
            foreach (var pattern in _patterns)
                pattern.Draw(context);
        }

        public bool Visible
        {
            get => _visible.Get();
            set => _visible.Set(value);
        }

        public IReadOnlyList<TypePattern> Patterns => _patterns;

        public Element CreateElement()
        {
            return UI.Tabs(
                ("Patterns", CreatePatternsTab),
                ("Words", _words.CreateElement),
                ("Display", CreateDisplayTab));
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }

        private bool TryInitialize()
        {
            var font = GetComponent<TypeLayoutFont>();
            if (!_outputCamera || font.Face == null)
            {
                Debug.LogError($"{nameof(TypeLayoutPerformance)} requires an output {nameof(Camera)} and a loaded font.",
                    this);
                return false;
            }

            _canvas = GetComponent<TypeLayoutCanvas>();
            _canvas.Initialize(_outputCamera);
            _drawer = new TypeLayoutDrawer(_canvas, font.Face);
            return true;
        }

        private Element CreatePatternsTab()
        {
            var elements = new List<Element>
            {
                UI.Row(
                    UI.Button("All Off", SetAllPatternsOff),
                    UI.Label(() => $"{CountEnabled()} / {_patterns.Length} on")),
                UI.Space()
            };
            foreach (var pattern in _patterns)
                elements.Add(pattern.CreateElement());

            return UI.Column(elements);
        }

        private Element CreateDisplayTab()
        {
            return UI.Column(
                _visible.CreateElement("Visible"),
                _color.CreateElement("Color"),
                _margin.CreateSlider("Safe Area Margin", 0f, 300f),
                _showGuides.CreateElement("Guides"),
                UI.Label("Guides: cyan = safe area, yellow = paths, magenta = glyph anchors."));
        }

        private void SetAllPatternsOff()
        {
            foreach (var pattern in _patterns)
                pattern.Enabled = false;
        }

        private int CountEnabled()
        {
            var count = 0;
            foreach (var pattern in _patterns)
            {
                if (pattern.Enabled) count++;
            }

            return count;
        }
    }
}
