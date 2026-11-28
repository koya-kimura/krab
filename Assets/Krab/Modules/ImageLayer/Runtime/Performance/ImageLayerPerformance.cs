using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Views;
using Krab.Common.Timing;
using Krab.Common.Views;
using Krab.ImageLayer.Core.Presets;
using Krab.ImageLayer.Performance.Patterns;
using Krab.ImageLayer.Unity.Library;
using Krab.ImageLayer.Unity.Loading;
using Krab.ImageLayer.Unity.Rendering;
using PrefsGUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// Cut-out images for performances. Two decks each draw one layout preset (how images are arranged) with one
    /// image entry (which images), both picked on radio groups that MIDI pads can switch live. Loads one library
    /// from StreamingAssets; presets live in JSON beside it.
    /// </summary>
    [UiTarget("ImageLayer/Performance", "Image Layer", 0)]
    public sealed partial class ImageLayerPerformance : MonoBehaviour, IUiTarget
    {
        // Used only when no tempo clock is injected, so patterns still move in a bare Scene.
        private const float FallbackBpm = 120;
        private const float ThresholdSoftness = 0.03f;

        [SerializeField] private ImageLayerCanvas _images;

        private readonly PrefsString _libraryName = new("ImageLayer Library", "Sequences");
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

        private readonly ImageDeck[] _decks = { new("A"), new("B") };
        private readonly Dictionary<(ImageSet, int), ImageSet> _singleClipSets = new();
        private readonly List<string> _setNames = new();

        private ImageLibrary _library;
        private CancellationTokenSource _loading;
        private IBPMManager _bpmManager;
        private MidiBindingManager _midiBindings;
        private IMidiBindingUi _midiUi;
        private float _progress;
        private string _loadError;
        private string _issuesText = string.Empty;

        private void Start()
        {
            LoadLayouts();
            foreach (var deck in _decks)
                deck.AttachMidi(_midiBindings);

            Reload();
        }

        private void Update()
        {
            if (!_images) return;

            var canvas = _images.Canvas;
            if (canvas == null) return;

            canvas.Reset();
            if (_library == null) return;

            foreach (var deck in _decks)
                DrawDeck(deck, canvas);
        }

        private void OnDestroy()
        {
            foreach (var deck in _decks)
                deck.Dispose();

            CancelLoading();
            _library?.Dispose();
            _library = null;
        }

        public ImageLibrary Library => _library;

        public bool IsLoading => _loading != null;

        public float Progress => _progress;

        public IReadOnlyList<ImageDeck> Decks => _decks;

        [Inject]
        public void Construct(IBPMManager bpmManager, [Nullable] IMidiBindingProvider midi,
            [Nullable] IMidiBindingUi midiUi)
        {
            _bpmManager = bpmManager;
            _midiBindings = midi?.Bindings;
            _midiUi = midiUi;
        }

        /// <summary>Loads the library named in the settings. The current one stays on screen until it finishes.</summary>
        public void Reload()
        {
            CancelLoading();
            _loading = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LoadAsync(_libraryName.Get(), _loading.Token);
        }

        private void DrawDeck(ImageDeck deck, ImageCanvas canvas)
        {
            var layoutKey = deck.LayoutKey;
            if (layoutKey == ImageDeck.OffKey)
            {
                deck.Error = null;
                return;
            }

            var layout = FindLayout(layoutKey);
            var pattern = layout == null ? null : FindPattern(layout.Pattern);
            var entry = FindImage(deck.ImageKey);
            if (pattern == null || entry == null)
            {
                deck.Error = layout == null ? "No layout selected."
                    : pattern == null ? $"Unknown pattern '{layout.Pattern}'."
                    : "No image selected.";
                return;
            }

            if (!_library.TryGet(entry.SetName, out var set))
            {
                deck.Error = $"Set '{entry.SetName}' is not in library '{_library.Name}'.";
                return;
            }

            if (entry.Clip != ImageEntry.AllClips)
            {
                if (entry.Clip >= set.ClipCount)
                {
                    deck.Error = $"Set '{set.Name}' has no clip {entry.Clip} (0..{set.ClipCount - 1}).";
                    return;
                }

                set = SingleClipSet(set, entry.Clip);
            }

            var camera = canvas.Camera;
            var beats = _bpmManager?.Beat ?? Time.timeAsDouble * FallbackBpm / 60;
            var bpm = _bpmManager?.CurrentBpm ?? FallbackBpm;
            var context = new ImagePatternContext(canvas, _library, camera.pixelWidth, camera.pixelHeight, beats, bpm,
                _dark.Get(), _light.Get(), Mathf.Clamp01(_thresholdLevel.Get()), entry.FramesPerBeat);

            canvas.Push();
            try
            {
                ApplyLook(canvas, ParseLook(entry.Look), context);
                deck.Error = pattern.Draw(context, set, layout.Values);
            }
            finally
            {
                canvas.Pop();
            }
        }

        /// <summary>The set narrowed to one clip, cached so drawing every frame does not allocate.</summary>
        private ImageSet SingleClipSet(ImageSet set, int clip)
        {
            if (_singleClipSets.TryGetValue((set, clip), out var single)) return single;

            single = new ImageSet(set.Name, set.Kind, new[] { set[clip] });
            _singleClipSets.Add((set, clip), single);
            return single;
        }

        private static ImageLook ParseLook(string look)
        {
            return Enum.TryParse(look, true, out ImageLook parsed) && Enum.IsDefined(typeof(ImageLook), parsed)
                ? parsed
                : ImageLook.Original;
        }

        private static void ApplyLook(ImageCanvas canvas, ImageLook look, in ImagePatternContext context)
        {
            switch (look)
            {
                case ImageLook.Duotone:
                    canvas.Duotone(context.Dark, context.Light);
                    break;
                case ImageLook.Threshold:
                    canvas.Duotone(context.Dark, context.Light);
                    canvas.Threshold(context.ThresholdLevel, ThresholdSoftness);
                    break;
            }
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
            _singleClipSets.Clear();
            _setNames.Clear();
            foreach (var set in _library.Sets)
                _setNames.Add(set.Name);

            var issues = new StringBuilder();
            foreach (var issue in _library.Issues)
                issues.AppendLine(issue.ToString());

            _issuesText = issues.ToString();
            if (_library.Issues.Count > 0)
                Debug.LogWarning($"{nameof(ImageLayerPerformance)}: '{_library.Name}' loaded with issues:\n{_issuesText}",
                    this);

            LoadImages();
        }

        private void CancelLoading()
        {
            if (_loading == null) return;

            _loading.Cancel();
            _loading.Dispose();
            _loading = null;
        }
    }
}
