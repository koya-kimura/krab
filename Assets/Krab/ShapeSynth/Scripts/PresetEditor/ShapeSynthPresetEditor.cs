using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Views;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Unity.Persistence;
using Krab.ShapeSynth.Unity.Runtime;
using RosettaUI;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Krab.ShapeSynth.PresetEditor
{
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Authoring", "Preset Editor", 100)]
    [MovedFrom("Krab.ShapeSynth.Samples.PresetEditor")]
    public sealed partial class ShapeSynthPresetEditor : MonoBehaviour, IUiTarget
    {
        private readonly Dictionary<string, int> _childrenRevisions = new();
        private readonly HashSet<string> _expandedGroups = new();

        private ShapeSynthController _controller;
        private PresetRepository _repository;
        private WindowElement _workspace;
        private string _selectedId;
        private string _selectedValueId;
        private string _error;
        private bool _dirty;
        private bool _openOnUpdate;
        private int _documentRevision;
        private int _valuesRevision;

        private void Awake()
        {
            _controller = GetComponent<ShapeSynthController>();
            EnsureDocument();
        }

        private void Start()
        {
            _openOnUpdate = true;
        }

        private void Update()
        {
            // Document is not serialized, so a script reload during Play leaves it empty.
            if (Document == null)
            {
                NewDocument();
                _error = "Script reload cleared the open preset.";
            }

            if (_openOnUpdate)
            {
                _openOnUpdate = false;
                ShowWorkspace();
            }

            AdvancePlayback(Time.unscaledDeltaTime);
            if (_refreshPreview && Time.realtimeSinceStartupAsDouble >= _previewDue) Preview();
        }

        private void OnDisable()
        {
            StopPreview();
            _workspace?.Close();
        }

        // The node tree is recursive; keep Unity's reload serialization from walking it. Files are the source of truth.
        [field: NonSerialized]
        public PresetDocument Document { get; private set; }

        public PresetRepository Repository => _repository ??= new PresetRepository();

        public string Error => _error;

        public bool IsDirty => _dirty;

        public PresetNode Selected => Document.Nodes().First(node => node.Id == _selectedId);

        private PresetValue SelectedValue => FindValue(_selectedValueId);

        /// <summary>A selected Group receives the child; a selected Shape adds a sibling into its parent Group.</summary>
        private PresetNode AddTarget =>
            Selected.Kind == PresetNodeKind.Group ? Selected : ParentOf(_selectedId) ?? Document.RootGroup;

        public void NewDocument()
        {
            Document = PresetDocument.CreateNew();
            ResetDocumentView();
            _dirty = true;
        }

        public void OpenDocument(string id)
        {
            Guard(() =>
            {
                var document = Repository.Load(id);
                // Older files may carry Multiplier / Absolute Value; fold them so the table shows real numbers.
                PresetUnits.Normalize(document);
                Document = document;
                ResetDocumentView();
                _dirty = false;
            });
        }

        public void SaveDocument()
        {
            Guard(() =>
            {
                Repository.Save(Document);
                _dirty = false;
                RefreshSavedPresets();
            });
        }

        public void SelectNode(string id)
        {
            _selectedId = id;
        }

        public void SelectValue(string id)
        {
            _selectedValueId = id;
        }

        public void AddNode(PresetNodeKind kind)
        {
            Guard(() =>
            {
                var parentId = AddTarget.Id;
                var child = Document.AddNode(parentId, kind);
                ChildrenChanged(parentId);
                _expandedGroups.Add(parentId);
                SelectNode(child.Id);
                Changed();
            });
        }

        public void DeleteSelected()
        {
            Guard(() =>
            {
                if (_selectedId == Document.RootGroup.Id) return;

                var parent = ParentOf(_selectedId);
                Document.DeleteNode(_selectedId);
                ChildrenChanged(parent.Id);
                SelectNode(parent.Id);
                Changed();
            });
        }

        private PresetValue FindValue(string id)
        {
            return id == null ? null : Document.Values.FirstOrDefault(value => value.Id == id);
        }

        private PresetNode ParentOf(string id)
        {
            return Document.Nodes().FirstOrDefault(node => node.Children.Any(child => child.Id == id));
        }

        private void EnsureDocument()
        {
            if (Document == null) NewDocument();
        }

        private void Changed()
        {
            _dirty = true;
            QueuePreview();
        }

        private void Guard(Action action)
        {
            try
            {
                action();
                _error = null;
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        private void ResetDocumentView()
        {
            _selectedId = Document.RootGroup.Children.FirstOrDefault()?.Id ?? Document.RootGroup.Id;
            _selectedValueId = Document.Values.FirstOrDefault()?.Id;
            _childrenRevisions.Clear();
            _expandedGroups.Clear();
            _expandedGroups.Add(Document.RootGroup.Id);
            _documentRevision++;
            _valuesRevision++;
            QueuePreview();
        }

        private int ChildRevision(string id)
        {
            return _childrenRevisions.TryGetValue(id, out var revision) ? revision : 0;
        }

        private void ChildrenChanged(string id)
        {
            _childrenRevisions[id] = ChildRevision(id) + 1;
        }
    }
}
