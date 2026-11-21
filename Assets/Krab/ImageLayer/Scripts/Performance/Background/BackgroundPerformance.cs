using System;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ImageLayer.Performance.Palette;
using Krab.TypeLayout.Unity.Rendering;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using Unite.Canvas;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ImageLayer.Performance.Background
{
    /// <summary>
    /// Flat background patterns behind the images, drawn with p5.Unite on the output camera in the shared palette.
    /// Sizes are reference pixels (1080 tall); motion follows the BPM clock.
    /// </summary>
    [RequireComponent(typeof(TypeLayoutCanvas))]
    [UiTarget("ImageLayer/Background", "Background", 2)]
    public sealed class BackgroundPerformance : MonoBehaviour, IUiTarget
    {
        private const float FallbackBpm = 120;
        private const float ReferenceHeight = 1080;
        private const int WarpSegments = 32;

        private static readonly string[] StyleNames = Enum.GetNames(typeof(BackgroundStyle));

        [SerializeField] private Camera _outputCamera;
        [SerializeField] private PaletteController _palette;

        private readonly PrefsInt _style = new("ImageLayer Background Style", (int)BackgroundStyle.Grid);
        private readonly PrefsFloat _cellSize = new("ImageLayer Background Cell Size", 90);
        private readonly PrefsFloat _lineWidth = new("ImageLayer Background Line Width", 2);
        private readonly PrefsFloat _cellsPerBeat = new("ImageLayer Background Cells Per Beat", 0);
        private readonly PrefsFloat _warp = new("ImageLayer Background Warp", 0.25f);

        private TypeLayoutCanvas _canvas;
        private IBPMManager _bpmManager;

        private void Awake()
        {
            _canvas = GetComponent<TypeLayoutCanvas>();
        }

        private void Update()
        {
            if (!_outputCamera || Style == BackgroundStyle.None) return;

            // Also covers a script reload during Play, which clears the canvas.
            if (!_canvas.IsInitialized) _canvas.Initialize(_outputCamera);

            var palette = _palette ? _palette.Current : ColorPalette.Presets[0];
            float width = _outputCamera.pixelWidth;
            float height = _outputCamera.pixelHeight;
            float unit = height / ReferenceHeight;
            float cell = Mathf.Max(4f, _cellSize.Get()) * unit;
            double beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            float scroll = (float)(beats * _cellsPerBeat.Get() % 2.0) * cell;

            var style = Style;
            _canvas.NoStroke();
            Fill(palette.Background);
            _canvas.Rect(0f, 0f, width, height);
            switch (style)
            {
                case BackgroundStyle.Grid:
                    DrawGrid(palette, width, height, cell, scroll, unit);
                    break;
                case BackgroundStyle.WarpGrid:
                    DrawWarpGrid(palette, width, height, cell, beats, unit);
                    break;
                case BackgroundStyle.Stripes:
                    DrawStripes(palette, width, height, cell, scroll);
                    break;
                case BackgroundStyle.Checker:
                    DrawChecker(palette, width, height, cell, scroll);
                    break;
            }
        }

        public BackgroundStyle Style
        {
            get => (BackgroundStyle)Math.Clamp(_style.Get(), 0, StyleNames.Length - 1);
            set => _style.Set((int)value);
        }

        public Element CreateElement()
        {
            return UI.Column(
                UI.Dropdown("Style", () => (int)Style, value => Style = (BackgroundStyle)value, StyleNames),
                _cellSize.CreateSlider("Cell Size", 10f, 400f),
                _lineWidth.CreateSlider("Line Width", 0.5f, 20f),
                _cellsPerBeat.CreateSlider("Cells Per Beat (scroll)", -2f, 2f),
                _warp.CreateSlider("Warp", 0f, 1f),
                UI.Label("Colors come from the Palette tab."));
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }

        // p5.Unite writes Fill/Stroke values without converting them to linear, so in this linear-space project
        // they come out brighter than given. Converting here keeps palette colors equal to the image looks,
        // whose shader receives colors through MaterialPropertyBlock.SetColor (converted by Unity).
        private void Fill(Color color)
        {
            _canvas.Fill(color.linear);
        }

        private void DrawGrid(in ColorPalette palette, float width, float height, float cell, float scroll, float unit)
        {
            float line = Mathf.Max(0.5f, _lineWidth.Get() * unit);
            float offset = Mathf.Repeat(scroll, cell);
            Fill(palette.Line);
            for (float x = offset - cell; x < width + cell; x += cell)
                _canvas.Rect(x - line / 2, 0f, line, height);

            for (float y = offset - cell; y < height + cell; y += cell)
                _canvas.Rect(0f, y - line / 2, width, line);
        }

        // Lines sway with a slow wave so the grid looks hand-drawn and alive without moving across the screen.
        private void DrawWarpGrid(in ColorPalette palette, float width, float height, float cell, double beats,
            float unit)
        {
            float amplitude = _warp.Get() * cell;
            float phase = (float)(beats * Math.PI / 2);
            float frequency = Mathf.PI * 2 / (cell * 6);
            _canvas.NoFill();
            _canvas.Stroke(palette.Line.linear);
            _canvas.StrokeWeight(Mathf.Max(0.5f, _lineWidth.Get() * unit));
            for (float x = 0f; x <= width + cell; x += cell)
            {
                _canvas.BeginShape();
                for (int segment = 0; segment <= WarpSegments; segment++)
                {
                    float y = height * segment / WarpSegments;
                    _canvas.Vertex(x + amplitude * Mathf.Sin(y * frequency + phase), y);
                }

                _canvas.EndShape();
            }

            for (float y = 0f; y <= height + cell; y += cell)
            {
                _canvas.BeginShape();
                for (int segment = 0; segment <= WarpSegments; segment++)
                {
                    float x = width * segment / WarpSegments;
                    _canvas.Vertex(x, y + amplitude * Mathf.Sin(x * frequency - phase));
                }

                _canvas.EndShape();
            }

            _canvas.NoStroke();
        }

        private void DrawStripes(in ColorPalette palette, float width, float height, float cell, float scroll)
        {
            float offset = Mathf.Repeat(scroll, cell * 2);
            Fill(palette.Accent);
            for (float y = offset - cell * 2; y < height; y += cell * 2)
                _canvas.Rect(0f, y, width, cell);
        }

        private void DrawChecker(in ColorPalette palette, float width, float height, float cell, float scroll)
        {
            float offset = Mathf.Repeat(scroll, cell * 2);
            Fill(palette.Accent);
            int row = 0;
            for (float y = offset - cell * 2; y < height; y += cell, row++)
            {
                float start = offset - cell * 2 + (row % 2 == 0 ? 0f : cell);
                for (float x = start; x < width; x += cell * 2)
                    _canvas.Rect(x, y, cell, cell);
            }
        }
    }
}
