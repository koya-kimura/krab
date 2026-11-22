using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ImageLayer.Performance.Patterns;
using Krab.ImageLayer.Unity.Library;
using Krab.ImageLayer.Unity.Loading;
using Krab.ImageLayer.Unity.Rendering;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// Cut-out images for performances: script-written patterns drawn on the image layer camera, switched and
    /// tuned from the settings UI and moving with the BPM clock. Loads one library from StreamingAssets.
    /// </summary>
    [UiTarget("ImageLayer/Performance", "Image Layer", 0)]
    public sealed class ImageLayerPerformance : MonoBehaviour, IUiTarget, IImageSetCatalog
    {
        // Used only when no tempo clock is injected, so patterns still move in a bare Scene.
        private const float FallbackBpm = 120;

        [SerializeField] private ImageLayerCanvas _images;

        private readonly PrefsBool _visible = new("ImageLayer Visible", true);
        private readonly PrefsString _libraryName = new("ImageLayer Library", "Maltine20");
        private readonly PrefsBool _compress = new("ImageLayer Compress", true);
        private readonly PrefsColor _dark = new("ImageLayer Dark", new Color(0.12f, 0.12f, 0.14f));
        private readonly PrefsColor _light = new("ImageLayer Light", Color.white);
        private readonly PrefsFloat _thresholdLevel = new("ImageLayer Threshold", 0.45f);

        private readonly ImagePattern[] _patterns =
        {
            new MarchPattern(),
            new LineupPattern(),
            new TilePattern(),
            new ScatterPattern(),
            new FallPattern(),
            new CenterPattern(),
            new MirrorPattern(),
            new OrbitPattern(),
            new TrailPattern(),
            new TunnelPattern(),
            new WavePattern(),
            new SpiralPattern(),
            new FlashPattern(),
            new PilePattern(),
            new ColumnsPattern(),
            new BouncePattern(),
            new RadialPattern(),
            new ParadePattern()
        };

        private readonly List<string> _setNames = new();

        private ImageLibrary _library;
        private CancellationTokenSource _loading;
        private IBPMManager _bpmManager;
        private float _progress;
        private string _loadError;
        private string _issuesText = string.Empty;

        private void Start()
        {
            foreach (var pattern in _patterns)
                pattern.Bind(this);

            Reload();
        }

        private void Update()
        {
            if (!_images) return;

            var canvas = _images.Canvas;
            if (canvas == null) return;

            canvas.Reset();
            if (!_visible.Get() || _library == null) return;

            var camera = canvas.Camera;
            var beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            var bpm = _bpmManager?.CurrentBpm ?? FallbackBpm;
            var context = new ImagePatternContext(canvas, _library, camera.pixelWidth, camera.pixelHeight, beats, bpm,
                _dark.Get(), _light.Get(), Mathf.Clamp01(_thresholdLevel.Get()));
            foreach (var pattern in _patterns)
                pattern.Draw(context);
        }

        private void OnDestroy()
        {
            CancelLoading();
            _library?.Dispose();
            _library = null;
        }

        public IReadOnlyList<string> SetNames => _setNames;

        public int Version { get; private set; }

        public ImageLibrary Library => _library;

        public bool IsLoading => _loading != null;

        public float Progress => _progress;

        public IReadOnlyList<ImagePattern> Patterns => _patterns;

        public bool Visible
        {
            get => _visible.Get();
            set => _visible.Set(value);
        }

        public Element CreateElement()
        {
            return UI.Tabs(
                ("Patterns", CreatePatternsTab),
                ("Library", CreateLibraryTab),
                ("Display", CreateDisplayTab));
        }

        [Inject]
        public void Construct(IBPMManager bpmManager)
        {
            _bpmManager = bpmManager;
        }

        /// <summary>Loads the library named in the settings. The current one stays on screen until it finishes.</summary>
        public void Reload()
        {
            CancelLoading();
            _loading = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LoadAsync(_libraryName.Get(), _loading.Token);
        }

        // async void: fire-and-forget from a UI button; every exception is handled here.
        private async void LoadAsync(string libraryName, CancellationToken cancellationToken)
        {
            var options = ImageLoadOptions.Default;
            options.Compress = _compress.Get();
            _progress = 0;
            _loadError = null;
            try
            {
                var library = await ImageLibraryLoader.LoadAsync(libraryName, options, value => _progress = value,
                    cancellationToken);
                _library?.Dispose();
                _library = library;
                OnLibraryLoaded();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                _loadError = exception.Message;
                Debug.LogException(exception, this);
            }

            _loading?.Dispose();
            _loading = null;
        }

        private void OnLibraryLoaded()
        {
            _setNames.Clear();
            foreach (var set in _library.Sets)
                _setNames.Add(set.Name);

            Version++;
            var issues = new StringBuilder();
            foreach (var issue in _library.Issues)
                issues.AppendLine(issue.ToString());

            _issuesText = issues.ToString();
            if (_library.Issues.Count > 0)
                Debug.LogWarning($"{nameof(ImageLayerPerformance)}: '{_library.Name}' loaded with issues:\n{_issuesText}",
                    this);
        }

        private void CancelLoading()
        {
            if (_loading == null) return;

            _loading.Cancel();
            _loading.Dispose();
            _loading = null;
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

        private Element CreateLibraryTab()
        {
            var libraries = new List<string>(ImageLibraryPaths.ListLibraries());
            if (!libraries.Contains(_libraryName.Get())) libraries.Insert(0, _libraryName.Get());

            return UI.Column(
                UI.Dropdown("Library", () => _libraryName.Get(), value => _libraryName.Set(value), libraries),
                _compress.CreateElement("Compress (DXT5, ~1/4 memory)"),
                UI.Button("Reload", Reload),
                UI.Label(StatusText),
                UI.DynamicElementIf(() => _issuesText.Length > 0,
                    () => UI.HelpBox(UI.Label(() => _issuesText), HelpBoxType.Warning)));
        }

        private Element CreateDisplayTab()
        {
            return UI.Column(
                _visible.CreateElement("Visible"),
                _dark.CreateElement("Dark"),
                _light.CreateElement("Light"),
                _thresholdLevel.CreateSlider("Threshold", 0f, 1f),
                UI.Label("Dark / Light / Threshold color the Duotone and Threshold looks of every pattern."));
        }

        private string StatusText()
        {
            if (_loadError != null) return $"Load failed: {_loadError}";
            if (_loading != null) return $"Loading {_libraryName.Get()}… {_progress:P0}";
            if (_library == null) return "No library loaded.";

            int frames = 0;
            foreach (var set in _library.Sets)
            foreach (var clip in set.Clips)
                frames += clip.FrameCount;

            return $"{_library.Name}: {_library.Sets.Count} sets, {frames} frames, " +
                   $"{_library.TextureBytes / 1048576.0:F0} MB, loaded in {_library.LoadSeconds:F1} s";
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
