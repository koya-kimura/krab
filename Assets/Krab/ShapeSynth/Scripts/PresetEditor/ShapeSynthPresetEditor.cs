using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Views;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Persistence;
using Krab.ShapeSynth.Unity.Runtime;
using RosettaUI;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Authoring", "Preset Editor", 100)]
    [MovedFrom("Krab.ShapeSynth.Samples.PresetEditor")]
    public sealed partial class ShapeSynthPresetEditor : MonoBehaviour, IUiTarget
    {
        private readonly Dictionary<string, int> _childrenRevisions = new();
        private readonly Dictionary<PresetParameter, int> _bindingRevisions = new();
        private readonly HashSet<string> _expandedGroups = new();

        private ShapeSynthController _controller;
        private PresetRepository _repository;
        private VoiceInstance _preview;
        private WindowElement _workspace;
        private PresetPreviewView _previewView;
        private PresetWorkspaceScale _workspaceScale;
        private string _selectedId;
        private string _selectedModulatorId;
        private string _status = "Ready.";
        private string _errorStatus;
        private bool _refreshPreview;
        private bool _hasPreview;
        private bool _dirty;
        private PresetPreviewMode _previewMode = PresetPreviewMode.Static;
        private bool _autoPreview = true;
        private bool _showValues;
        private float _beat = 0.8f;
        private bool _playing;
        private PreviewSignal[] _previewSignals = Array.Empty<PreviewSignal>();
        private bool _openOnUpdate;
        private double _previewDue;
        private float _bpm = 120;
        private int _seed = 1234;
        private int _documentRevision;
        private int _modulatorsRevision;
        private int _modulatorNamesRevision;
        private Action _pendingAction;
        private string _pendingDescription;

        private void Awake()
        {
            _controller = GetComponent<ShapeSynthController>();
            var document = FindFirstObjectByType<UIDocument>();
            if (document) _workspaceScale = new PresetWorkspaceScale(document);

            EnsureDocument();
        }

        private void Start()
        {
            _openOnUpdate = true;
        }

        private void Update()
        {
            _workspaceScale?.Update();
            if (_openOnUpdate)
            {
                _openOnUpdate = false;
                ShowWorkspace();
            }

            AdvancePlayback(Time.unscaledDeltaTime);
            if (!_refreshPreview || Time.realtimeSinceStartupAsDouble < _previewDue) return;

            _refreshPreview = false;
            if (_autoPreview && _hasPreview) Preview();
        }

        private void OnDisable()
        {
            StopPreview();
            _workspace?.Close();
        }

        private void OnDestroy()
        {
            _workspaceScale?.Dispose();
        }

        public PresetDocument Document { get; private set; }

        public PresetRepository Repository => _repository ??= new PresetRepository();

        public string Status => _status;

        public bool IsDirty => _dirty;

        public int InspectorBuildCount { get; private set; }

        public int PreviewCompileCount { get; private set; }

        public PresetPreviewView PreviewView => _previewView;

        public PresetNode Selected => Document.Nodes().First(node => node.Id == _selectedId);

        public PresetPreviewMode PreviewMode => _previewMode;

        /// <summary>True while something is on screen: a still preview, or an Animated voice that has not finished.</summary>
        public bool IsPreviewRunning => _playing && _hasPreview;

        public float Beat => _beat;

        /// <summary>Longest ADSR lifetime, capped to the slider range. ADSRs end the Voice, so the loop matches a trigger.</summary>
        public float LoopBeats
        {
            get
            {
                var lifetime = Document.Modulators
                    .Where(model => model.Kind == ModulatorKind.Adsr)
                    .Select(model => model.Attack + model.Decay + model.SustainTime + model.Release)
                    .Where(value => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0)
                    .DefaultIfEmpty(PresetSignalSampler.GraphBeats)
                    .Max();
                return Mathf.Clamp((float)lifetime, 0.25f, PresetSignalSampler.GraphBeats);
            }
        }

        private ModulatorSettings SelectedModulator =>
            Document.Modulators.FirstOrDefault(model => model.Id == _selectedModulatorId);

        /// <summary>A selected Group receives the child; a selected Shape adds a sibling into its parent Group.</summary>
        private PresetNode AddTarget =>
            Selected.Kind == PresetNodeKind.Group ? Selected : ParentOf(_selectedId) ?? Document.RootGroup;

        private bool CanDeleteNode => _selectedId != Document.RootGroup.Id;

        public void NewDocument()
        {
            Document = PresetDocument.CreateNew();
            ResetDocumentView();
            _dirty = true;
            _status = "New preset — not saved.";
        }

        public void OpenDocument(string id)
        {
            Guard(() =>
            {
                var loaded = Repository.Load(id);
                Document = loaded;
                ResetDocumentView();
                _dirty = false;
                _showOpen = false;
                _status = "Opened " + loaded.Name;
                Preview();
            });
        }

        public void SaveDocument()
        {
            Guard(() =>
            {
                Repository.Save(Document);
                _dirty = false;
                _status = "Saved " + Document.Name;
                RefreshSavedPresets();
            });
        }

        public void SelectNode(string id)
        {
            if (_selectedId != id) _editingParameter = null;

            _selectedId = id;
        }

        public void SelectModulator(string id)
        {
            _selectedModulatorId = id;
        }

        public void AddNode(PresetNodeKind kind)
        {
            Guard(() =>
            {
                var parentId = AddTarget.Id;
                var child = Document.AddNode(parentId, kind);
                _childrenRevisions[parentId] = ChildRevision(parentId) + 1;
                _expandedGroups.Add(parentId);
                SelectNode(child.Id);
                Changed();
            });
        }

        public void DeleteSelected()
        {
            Guard(() =>
            {
                if (!CanDeleteNode) throw new ArgumentException("Root Group cannot be deleted. Select a child Node.");

                var parent = ParentOf(_selectedId);
                Document.DeleteNode(_selectedId);
                _childrenRevisions[parent.Id] = ChildRevision(parent.Id) + 1;
                SelectNode(parent.Id);
                Changed();
            });
        }

        public void Preview()
        {
            Guard(() =>
            {
                _refreshPreview = false;
                var target = PresetCompiler.Compile(Document);
                VoiceInstance replacement = null;
                HierarchyEvaluator still = null;
                if (_previewMode != PresetPreviewMode.Animated)
                {
                    _previewSignals = Document.Modulators.Select(model => new PreviewSignal(model)).ToArray();
                    UpdatePreviewSignals();
                    still = new HierarchyEvaluator(target.Definition, _previewSignals, _seed);
                }
                else
                {
                    replacement = _controller.Spawn(target, _bpm, _seed);
                    _beat = 0; // A new Voice starts at its own beat 0; keep the cursor in step with it.
                }

                _controller.Collection.Stop(_preview);
                _preview = replacement;
                _controller.StaticPreview = still;
                _hasPreview = true;
                PreviewCompileCount++;
                _status = PreviewStatus();
            });
        }

        public void StopPreview()
        {
            if (_controller)
            {
                _controller.Collection.Stop(_preview);
                _controller.StaticPreview = null;
            }

            _preview = null;
            _hasPreview = false;
            _refreshPreview = false;
            _playing = false;
            _status = "Preview stopped.";
        }

        /// <summary>
        /// Preview button: plays the preview from beat 0 with time flowing at the current BPM, looping over one
        /// Lifetime. Static switches to Beat, because a fixed signal of 1 cannot move. Pressing again restarts.
        /// </summary>
        public void Play()
        {
            if (_previewMode == PresetPreviewMode.Static) _previewMode = PresetPreviewMode.Beat;

            _beat = 0;
            _playing = true;
            Preview();
            if (!_hasPreview)
            {
                _playing = false;
                return;
            }

            _status = $"Playing at {_bpm:G} BPM — loop {LoopBeats:F2} beats.";
        }

        /// <summary>Stop button: time stops. Beat keeps the current frame for scrubbing; Animated removes its Voice.</summary>
        public void StopPlayback()
        {
            if (!_playing) return;

            _playing = false;
            if (_previewMode == PresetPreviewMode.Animated) StopPreview();

            _status = $"Stopped at beat {_beat:F2}.";
        }

        public void SetPreviewMode(PresetPreviewMode mode)
        {
            if (_previewMode == mode) return;

            _previewMode = mode;
            if (mode == PresetPreviewMode.Static) _playing = false;
            if (_playing && mode == PresetPreviewMode.Animated) _beat = 0;
            if (_hasPreview || _autoPreview) Preview();
        }

        /// <summary>Scrubs (or, while playing, seeks) without recompiling. Static switches to Beat, as the slider implies it.</summary>
        public void SetBeat(float beat)
        {
            _beat = Mathf.Clamp(beat, 0, PresetSignalSampler.GraphBeats);
            if (_previewMode == PresetPreviewMode.Static)
            {
                SetPreviewMode(PresetPreviewMode.Beat);
                return;
            }

            if (_previewMode != PresetPreviewMode.Beat) return;

            UpdatePreviewSignals();
            if (_hasPreview && !_playing) _status = PreviewStatus();
        }

        private PresetNode ParentOf(string id)
        {
            return Document.Nodes().FirstOrDefault(node => node.Children.Any(child => child.Id == id));
        }

        private void EnsureDocument()
        {
            if (Document == null) NewDocument();
        }

        private void Changed(bool preview = true)
        {
            _dirty = true;
            if (!preview) return;

            QueuePreview();
        }

        private void QueuePreview()
        {
            _refreshPreview = true;
            _previewDue = Time.realtimeSinceStartupAsDouble + 0.3;
        }

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _errorStatus = "⚠ " + exception.Message;
                _status = _errorStatus;
            }
        }

        private void ConfirmIfDirty(string description, Action action)
        {
            if (!_dirty)
            {
                action();
                return;
            }

            _pendingDescription = description;
            _pendingAction = action;
        }

        private void ResetDocumentView()
        {
            _selectedId = Document.RootGroup.Children.FirstOrDefault()?.Id ?? Document.RootGroup.Id;
            _selectedModulatorId = Document.Modulators.FirstOrDefault()?.Id;
            _childrenRevisions.Clear();
            _bindingRevisions.Clear();
            _expandedGroups.Clear();
            _editingParameter = null;
            _pendingSignals.Clear();
            _pendingDepths.Clear();
            _expandedGroups.Add(Document.RootGroup.Id);
            _documentRevision++;
            _pendingAction = null;
            StopPreview();
        }

        private int ChildRevision(string id)
        {
            return _childrenRevisions.TryGetValue(id, out var revision) ? revision : 0;
        }

        private int BindingRevision(PresetParameter parameter)
        {
            return _bindingRevisions.TryGetValue(parameter, out var revision) ? revision : 0;
        }

        private void BindingStructureChanged(PresetParameter parameter)
        {
            _bindingRevisions[parameter] = BindingRevision(parameter) + 1;
            Changed();
        }

        private void AdvancePlayback(float deltaSeconds)
        {
            if (!_playing || !_hasPreview || _bpm <= 0) return;

            _beat += deltaSeconds * _bpm / 60f;
            if (_previewMode == PresetPreviewMode.Animated)
            {
                // The real Voice follows the same BPM; when it ends naturally, trigger it again.
                var alive = _controller && _preview != null && _controller.Collection.Voices.Contains(_preview);
                if (!alive || _beat > PresetSignalSampler.GraphBeats)
                {
                    _beat = 0;
                    Preview();
                }

                return;
            }

            var loop = LoopBeats;
            if (_beat >= loop) _beat %= loop;

            UpdatePreviewSignals();
        }

        private void UpdatePreviewSignals()
        {
            foreach (var signal in _previewSignals)
                signal.Value = _previewMode == PresetPreviewMode.Static ? 1 : PresetSignalSampler.SampleOrZero(signal.Model, _beat);
        }

        /// <summary>Signal value the Inspector shows for a binding: 1 in Static, otherwise the value at the scrub Beat.</summary>
        private double DisplaySignal(string modulatorId)
        {
            if (_previewMode == PresetPreviewMode.Static) return 1;

            var model = Document.Modulators.FirstOrDefault(entry => entry.Id == modulatorId);
            return model == null ? 0 : PresetSignalSampler.SampleOrZero(model, _beat);
        }

        private string PreviewStatus()
        {
            if (_previewMode == PresetPreviewMode.Static) return "Static Preview — every signal is fixed at 1.";
            if (_playing) return $"Playing at {_bpm:G} BPM — loop {LoopBeats:F2} beats.";
            if (_previewMode == PresetPreviewMode.Beat)
                return $"Beat Preview — signals sampled at beat {_beat:F2}. Press Preview to play.";

            return "Animated Preview restarted.";
        }

        /// <summary>A still signal whose value can change without recompiling the hierarchy.</summary>
        private sealed class PreviewSignal : IModulator
        {
            public PreviewSignal(ModulatorSettings model)
            {
                Model = model;
            }

            public ModulatorSettings Model { get; }

            public double Value { get; set; } = 1;

            public double Evaluate(double nowSeconds)
            {
                return Value;
            }
        }
    }

    public enum PresetPreviewMode { Static, Beat, Animated }
}
