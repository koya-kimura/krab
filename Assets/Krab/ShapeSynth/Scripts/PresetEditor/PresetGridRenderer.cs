using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Renders clipped preset previews into cells of one texture.</summary>
    public sealed class PresetGridRenderer : IDisposable
    {
        public const int Columns = 6;
        public const int CellWidth = 320;
        public const int CellHeight = 180;

        private const int PreviewWidth = 1920;
        private const int PreviewHeight = 1080;
        private const int DefaultSeed = 1234;
        private const int PreviewLayer = 31;
        private const double CellScale = (double)CellWidth / PreviewWidth;

        private static readonly ReferenceContext PreviewContext = new(PreviewWidth, PreviewHeight);
        private static readonly TransformDefinition CellTransform =
            new(scaleX: ParameterDefinition.Constant(CellScale), scaleY: ParameterDefinition.Constant(CellScale));

        private readonly List<Entry> _entries = new();
        private readonly Dictionary<Camera, Cell> _cellsByCamera = new();
        private readonly List<Cell> _cells = new();
        private RenderTexture _texture;
        private GameObject _root;
        private bool _disposed;
        private string _compileError;

        public Texture Texture => _texture;

        public string Error { get; private set; }

        public int Count => _entries.Count;

        public int Rows => (Count + Columns - 1) / Columns;

        /// <summary>Returns the cell's atlas rectangle in pixels, with its origin at the top left.</summary>
        public static RectInt GetCellRect(int index)
        {
            return new RectInt(index % Columns * CellWidth, index / Columns * CellHeight, CellWidth, CellHeight);
        }

        public void SetPresets(IReadOnlyList<PresetDocument> presets)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PresetGridRenderer));
            if (presets == null) throw new ArgumentNullException(nameof(presets));

            _entries.Clear();
            var errors = new List<string>();
            foreach (var document in presets)
            {
                try
                {
                    _entries.Add(new Entry(document));
                }
                catch (ArgumentException exception)
                {
                    errors.Add($"{document?.Name}: {exception.Message}");
                    _entries.Add(null);
                }
            }

            _compileError = string.Join("\n", errors);
            Error = _compileError;
            UpdateTargets();
        }

        /// <summary>Each cell's seed moves on with every loop of that cell, so Rand differs on each replay.</summary>
        public void Tick(double elapsedBeats, bool loop)
        {
            if (_disposed || !_texture || double.IsNaN(elapsedBeats) || double.IsInfinity(elapsedBeats)) return;

            Error = _compileError;
            elapsedBeats = Math.Max(0, elapsedBeats);
            for (var index = 0; index < _entries.Count; index++)
            {
                var entry = _entries[index];
                if (entry == null) continue;

                try
                {
                    var canvas = _cells[index].Canvas;
                    canvas.EnsureInitialized();
                    var beat = loop ? elapsedBeats % entry.LoopBeats : Math.Min(elapsedBeats, entry.LoopBeats);
                    var loops = loop ? Math.Min(int.MaxValue - DefaultSeed, elapsedBeats / entry.LoopBeats) : 0;
                    var seed = DefaultSeed + (int)loops;
                    UniteShapeAdapter.Draw(canvas, entry.Evaluate(beat, seed), CellWidth, CellHeight, true);
                }
                catch (ArgumentException exception)
                {
                    var error = $"{entry.Name}: {exception.Message}";
                    Error = string.IsNullOrEmpty(Error) ? error : $"{Error}\n{error}";
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _entries.Clear();
            UpdateTargets();
            if (_root) Destroy(_root);
            _disposed = true;
        }

        private void UpdateTargets()
        {
            if (_cells.Count == Count) return;

            RenderPipelineManager.endCameraRendering -= CopyCellToAtlas;
            while (_cells.Count > Count)
            {
                var index = _cells.Count - 1;
                var cell = _cells[index];
                _cellsByCamera.Remove(cell.Camera);
                cell.Dispose();
                _cells.RemoveAt(index);
            }

            if (_texture)
            {
                Release(_texture);
                _texture = null;
            }

            if (Count == 0) return;

            if (!_root) _root = new GameObject("Preset Gallery Cameras") { hideFlags = HideFlags.HideAndDontSave };

            _texture = CreateTarget("Preset Gallery", Columns * CellWidth, Rows * CellHeight, 0);
            var previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = _texture;
                GL.Clear(false, true, Color.black);
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }

            for (var index = _cells.Count; index < Count; index++)
            {
                var cell = new Cell(_root.transform, index);
                _cells.Add(cell);
                _cellsByCamera.Add(cell.Camera, cell);
            }

            RenderPipelineManager.endCameraRendering += CopyCellToAtlas;
        }

        private void CopyCellToAtlas(ScriptableRenderContext context, Camera camera)
        {
            if (!_texture || !_cellsByCamera.TryGetValue(camera, out var cell)) return;

            var rect = cell.Rect;
            var bottom = _texture.height - rect.yMax;
            var commands = CommandBufferPool.Get("Copy Preset Gallery Cell");
            try
            {
                commands.CopyTexture(cell.Target, 0, 0, 0, 0, CellWidth, CellHeight, _texture, 0, 0, rect.x, bottom);
                context.ExecuteCommandBuffer(commands);
            }
            finally
            {
                CommandBufferPool.Release(commands);
            }
        }

        private static RenderTexture CreateTarget(string name, int width, int height, int depth)
        {
            var target = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            target.Create();
            return target;
        }

        private static void Release(RenderTexture texture)
        {
            texture.Release();
            Destroy(texture);
        }

        private static void Destroy(Object value)
        {
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }

        private sealed class Cell : IDisposable
        {
            public Cell(Transform parent, int index)
            {
                Rect = GetCellRect(index);
                var name = $"Preset Gallery Cell {index + 1}";
                Target = CreateTarget(name, CellWidth, CellHeight, 24);
                var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave, layer = PreviewLayer };
                root.transform.SetParent(parent, false);
                Camera = root.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = Color.black;
                // RenderMeshIndirect obeys the camera mask even for ScreenOverlay draws.
                Camera.cullingMask = 1 << PreviewLayer;
                Camera.depth = -10;
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                Camera.targetTexture = Target;
                Canvas = root.AddComponent<TargetCameraCanvas>();
                Canvas.Initialize(Camera);
                Camera.enabled = true;
            }

            public RectInt Rect { get; }

            public RenderTexture Target { get; }

            public Camera Camera { get; }

            public TargetCameraCanvas Canvas { get; }

            public void Dispose()
            {
                Canvas.enabled = false;
                Camera.enabled = false;
                Camera.targetTexture = null;
                Release(Target);
                Destroy(Camera.gameObject);
            }
        }

        private sealed class Entry
        {
            private readonly GroupDefinition _definition;
            private readonly IReadOnlyList<IModulator> _signals;
            private HierarchyEvaluator _evaluator;
            private int _seed;

            public Entry(PresetDocument document)
            {
                var compiled = PresetCompiler.Compile(document);
                _definition = new GroupDefinition(CellTransform, compiled.Definition);
                Name = document.Name;

                var components = PresetCompiler.Components(document).ToArray();
                // At 60 BPM, elapsed seconds are numerically equal to elapsed beats.
                var time = new VoiceTime(0, 60);
                _signals = components.Select(model => PresetCompiler.ModulatorDefinition(model).Create(time)).ToArray();
                _seed = DefaultSeed;
                _evaluator = new HierarchyEvaluator(_definition, _signals, _seed);
                var lifetime = components.Where(model => model.Kind == ModulatorKind.Adsr)
                    .Max(model => model.Attack + model.Decay + model.SustainTime + model.Release);
                var delay = document.Nodes().Max(node => PresetCopyPattern.MaxDelayBeats(node.Layout));
                LoopBeats = Math.Clamp(lifetime + delay, 0.25, PresetSignalSampler.MaxPreviewBeats);
            }

            public string Name { get; }

            public double LoopBeats { get; }

            public IReadOnlyList<DrawItem> Evaluate(double beat, int seed)
            {
                if (_seed != seed)
                {
                    _evaluator = new HierarchyEvaluator(_definition, _signals, seed);
                    _seed = seed;
                }

                return _evaluator.Evaluate(beat, PreviewContext);
            }
        }
    }
}
