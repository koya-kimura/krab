using System;
using System.Text;
using Krab.Common.Timing;
using Krab.ImageLayer.Core.Frames;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using Krab.ImageLayer.Unity.Loading;
using Krab.ImageLayer.Unity.Rendering;
using Krab.TypeLayout.Unity.Rendering;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ImageLayer.Samples
{
    /// <summary>
    /// Loads an image library and draws a fixed demo on three stacked layers: p5.Unite background (output camera),
    /// images (overlay), p5.Unite foreground (overlay). Shows load progress, memory and issues on screen.
    /// </summary>
    public sealed class ImageLayerSmokeTest : MonoBehaviour
    {
        private const float FallbackBpm = 120;
        private const float ReferenceHeight = 1080;

        private static readonly Color Paper = new(0.98f, 0.84f, 0.2f);
        private static readonly Color Ink = new(0.12f, 0.12f, 0.14f);
        private static readonly Color Pink = new(0.95f, 0.3f, 0.75f);
        private static readonly Color Navy = new(0.16f, 0.2f, 0.55f);

        [SerializeField] private string _libraryName = "Sequences";
        [SerializeField] private ImageLoadOptions _loadOptions = ImageLoadOptions.Default;
        [SerializeField] private Camera _outputCamera;
        [SerializeField] private TypeLayoutCanvas _background;
        [SerializeField] private ImageLayerCanvas _images;
        [SerializeField] private TypeLayoutCanvas _foreground;
        [Tooltip("Draws plain markers in the top-left corner of each layer instead of the demo, for orientation checks.")]
        [SerializeField] private bool _orientationCheck;

        private ImageLibrary _library;
        private IBPMManager _bpmManager;
        private float _progress;
        private string _error;

        private async void Start()
        {
            // async void: Start is the Unity entry point; exceptions are caught below.
            try
            {
                _library = await ImageLibraryLoader.LoadAsync(_libraryName, _loadOptions, OnProgress,
                    destroyCancellationToken);
                LogSummary();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void Update()
        {
            if (!_outputCamera || !_background || !_images || !_foreground) return;

            EnsureCanvas(_background, _outputCamera);
            EnsureCanvas(_foreground, _foreground.TargetCamera);
            var canvas = _images.Canvas;
            if (canvas == null) return;

            canvas.Reset();
            float width = _outputCamera.pixelWidth;
            float height = _outputCamera.pixelHeight;
            if (_orientationCheck)
            {
                DrawOrientationCheck(canvas, width, height);
                return;
            }

            DrawBackground(width, height);
            if (_library == null)
            {
                DrawProgress(width, height);
                return;
            }

            double beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            float unit = height / ReferenceHeight;
            DrawPenguinRow(canvas, beats, width, unit);
            DrawWalkers(canvas, beats, width, unit);
            DrawStills(canvas, beats, width, height, unit);
            DrawForeground(beats, width, height, unit);
        }

        private void OnDestroy()
        {
            _library?.Dispose();
            _library = null;
        }

        public ImageLibrary Library => _library;

        public float Progress => _progress;

        public string Error => _error;

        public bool OrientationCheck
        {
            get => _orientationCheck;
            set => _orientationCheck = value;
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }

        private void OnProgress(float progress) => _progress = progress;

        private static void EnsureCanvas(TypeLayoutCanvas canvas, Camera camera)
        {
            // A script reload during Play clears the canvas without calling Start again.
            if (!canvas.IsInitialized && camera) canvas.Initialize(camera);
        }

        private void DrawBackground(float width, float height)
        {
            _background.NoStroke();
            _background.Fill(Paper);
            _background.Rect(0f, 0f, width, height);

            float spacing = height / 12f;
            float line = Mathf.Max(1f, height / 540f);
            _background.Fill(new Color(Ink.r, Ink.g, Ink.b, 0.35f));
            for (float x = spacing * 0.5f; x < width; x += spacing)
                _background.Rect(x, 0f, line, height);

            for (float y = spacing * 0.5f; y < height; y += spacing)
                _background.Rect(0f, y, width, line);
        }

        private void DrawProgress(float width, float height)
        {
            float barWidth = width * 0.5f;
            float barHeight = height * 0.02f;
            float x = (width - barWidth) / 2;
            float y = (height - barHeight) / 2;
            _foreground.NoStroke();
            _foreground.Fill(Ink);
            _foreground.Rect(x, y, barWidth, barHeight);
            _foreground.Fill(Pink);
            _foreground.Rect(x, y, barWidth * _progress, barHeight);
        }

        // Penguins march to the right in a row, fitted to cells so their size never changes.
        private void DrawPenguinRow(ImageCanvas canvas, double beats, float width, float unit)
        {
            if (!_library.TryGet("animal", out var animals)) return;

            var penguins = animals[0];
            float cell = 220f * unit;
            float offset = (float)(beats * 0.5 % 1.0) * cell;
            canvas.Push();
            canvas.Duotone(Ink, Color.white);
            canvas.Threshold(0.45f, 0.05f);
            for (float x = -cell; x < width + cell; x += cell)
                canvas.ImageFit(penguins, penguins[0], x + offset, 40f * unit, cell, cell * 1.1f);

            canvas.Pop();
        }

        // Four walkers loop through their frames in original colors, one beat per stride.
        private void DrawWalkers(ImageCanvas canvas, double beats, float width, float unit)
        {
            if (!_library.TryGet("walk", out var walkers)) return;

            float cellWidth = width / walkers.ClipCount;
            float cellHeight = 360f * unit;
            for (int index = 0; index < walkers.ClipCount; index++)
            {
                var clip = walkers[index];
                var frame = clip.AtProgress(beats * 0.5 + index * 0.25, FrameWrap.Loop);
                canvas.ImageFit(clip, frame, cellWidth * index, 300f * unit, cellWidth, cellHeight, FitMode.Contain,
                    0.5f, 1f);
            }
        }

        // Stills step through the set once per beat, mapped to navy and pink.
        private void DrawStills(ImageCanvas canvas, double beats, float width, float height, float unit)
        {
            if (!_library.TryGet("life", out var life)) return;

            const int columns = 6;
            float cellWidth = width / columns;
            float cellHeight = 300f * unit;
            int step = (int)Math.Floor(beats);
            canvas.Push();
            canvas.Duotone(Navy, Pink);
            for (int column = 0; column < columns; column++)
            {
                var clip = life.Wrap(step + column);
                canvas.ImageFit(clip, clip[0], cellWidth * column, height - cellHeight - 30f * unit, cellWidth,
                    cellHeight);
            }

            canvas.Pop();
        }

        private void DrawForeground(double beats, float width, float height, float unit)
        {
            _foreground.NoStroke();
            _foreground.Fill(Pink);
            float phase = (float)(beats % 4.0) / 4f;
            for (int index = 0; index < 5; index++)
            {
                float t = (index + phase) / 5f;
                _foreground.Circle(width * t, height * 0.32f, (40f + 30f * Mathf.Sin(t * Mathf.PI)) * unit);
            }
        }

        // Each layer marks the top-left quarter: background red, images green (inset), foreground blue (smaller).
        private void DrawOrientationCheck(ImageCanvas canvas, float width, float height)
        {
            _background.NoStroke();
            _background.Fill(Color.black);
            _background.Rect(0f, 0f, width, height);
            _background.Fill(Color.red);
            _background.Rect(0f, 0f, width / 2, height / 2);

            if (_library != null && _library.TryGet("life", out var life))
            {
                canvas.Duotone(Color.green, Color.green);
                var clip = life[0];
                canvas.Image(clip[0], width * 0.1f, height * 0.1f, width * 0.3f, height * 0.3f);
            }

            _foreground.NoStroke();
            _foreground.Fill(Color.blue);
            _foreground.Rect(0f, 0f, width * 0.08f, height * 0.08f);
        }

        private void LogSummary()
        {
            var summary = new StringBuilder();
            int frames = 0;
            foreach (var set in _library.Sets)
            foreach (var clip in set.Clips)
                frames += clip.FrameCount;

            summary.Append($"{nameof(ImageLayerSmokeTest)}: loaded '{_library.Name}' — {_library.Sets.Count} sets, ");
            summary.Append($"{frames} frames, {_library.TextureBytes / 1048576.0:F0} MB, {_library.LoadSeconds:F1} s");
            foreach (var issue in _library.Issues)
                summary.Append($"\n  {issue}");

            Debug.Log(summary.ToString(), this);
        }
    }
}
