using System;
using System.Collections.Generic;
using System.Linq;
using Krab.Common.Views;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Unity.Persistence;
using Krab.ShapeSynth.Unity.Runtime;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Krab.ShapeSynth.PresetEditor
{
    [RequireComponent(typeof(ShapeSynthController))]
    [UiTarget("ShapeSynth/Authoring", "Preset Editor", 100)]
    [MovedFrom("Krab.ShapeSynth.Samples.PresetEditor")]
    public sealed partial class ShapeSynthPresetEditor : MonoBehaviour, IUiTarget, IUiWindowsTarget, IUiSaveTarget
    {
        private readonly Dictionary<string, int> _childrenRevisions = new();
        private readonly HashSet<string> _expandedGroups = new();

        private ShapeSynthController _controller;
        private PresetRepository _repository;
        private List<PresetValue> _values;
        private string _valuesLoadError;

        // Values.json as last read or written by this editor, to tell a change on disk from an edit here.
        private string _valuesFileSignature;
        private string _valuesSnapshot;
        private string _selectedId;
        private string _selectedValueId;
        private string _error;
        private string _valueError;
        private bool _dirty;
        private int _documentRevision;
        private int _valuesRevision;

        private void Awake()
        {
            _controller = GetComponent<ShapeSynthController>();
            EnsureDocument();
        }

        private void Start()
        {
            RefreshSavedPresets();
        }

        private void Update()
        {
            // Document is not serialized, so a script reload during Play leaves it empty.
            if (Document == null)
            {
                NewDocument();
                _error = "Script reload cleared the open preset.";
            }

            RefreshSavedPresetsWhenFilesChange();
            UpdateGallery();
            AdvancePlayback(Time.unscaledDeltaTime);
            if (_refreshPreview && Time.realtimeSinceStartupAsDouble >= _previewDue) Preview();
        }

        private void OnDisable()
        {
            StopPreview();
            DisposeGallery();
        }

        // The node tree is recursive; keep Unity's reload serialization from walking it. Files are the source of truth.
        [field: NonSerialized]
        public PresetDocument Document { get; private set; }

        public PresetRepository Repository => _repository ??= new PresetRepository();

        /// <summary>
        /// The shared Value library every opened preset is attached to, so the Values window lists the same Values
        /// whichever preset is open. Read once; a script reload reads it again.
        /// </summary>
        public List<PresetValue> Values => _values ??= LoadValues();

        public string Error => _error;

        public bool IsDirty => _dirty;

        public PresetNode Selected => Document.Nodes().First(node => node.Id == _selectedId);

        private PresetValue SelectedValue => FindValue(_selectedValueId);

        /// <summary>A selected Group receives the child; a selected Shape adds a sibling into its parent Group.</summary>
        private PresetNode AddTarget =>
            Selected.Kind == PresetNodeKind.Group ? Selected : ParentOf(_selectedId) ?? Document.RootGroup;

        public void NewDocument()
        {
            Document = PresetDocument.CreateNew(Values);
            ResetDocumentView();
            _dirty = true;
        }

        public void OpenDocument(string id)
        {
            Guard(() =>
            {
                var document = Repository.Load(id, Values);
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
                // An unreadable library is kept as is, so a save cannot replace it with the empty fallback.
                if (_valuesLoadError != null)
                    throw new InvalidOperationException(_valuesLoadError + " Fix Values.json before saving.");

                // Values first: the preset's Bindings must find their Values when another reader loads it.
                Repository.SaveValues(Values);
                RememberValuesOnDisk();
                Repository.Save(Document);
                _dirty = false;
            });

            if (!_dirty) RefreshSavedPresets();
        }

        /// <summary>
        /// The Settings Save button and Cmd+S: writes the shared Values and the open preset only when they have
        /// unsaved changes.
        /// </summary>
        public void Save()
        {
            if (_dirty) SaveDocument();
        }

        /// <summary>Opens an unsaved copy of the open preset, named "… Copy"; save it to keep it.</summary>
        public void DuplicateDocument()
        {
            Guard(() =>
            {
                var names = _savedPresets.Select(preset => preset.Name).Append(Document.Name);
                Document = Repository.Duplicate(Document, UniqueName(Document.Name + " Copy", names));
                ResetDocumentView();
                _dirty = true;
            });
        }

        /// <summary>The name itself when free, otherwise the name followed by the first free number from 2.</summary>
        private static string UniqueName(string name, IEnumerable<string> taken)
        {
            var names = new HashSet<string>(taken);
            if (!names.Contains(name)) return name;

            var number = 2;
            while (names.Contains(name + " " + number)) number++;
            return name + " " + number;
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
            return id == null ? null : (Document?.AllValues() ?? Values).FirstOrDefault(value => value.Id == id);
        }

        private List<PresetValue> LoadValues()
        {
            try
            {
                _valuesLoadError = null;
                var values = Repository.LoadValues();
                RememberValuesOnDisk(values);
                return values;
            }
            catch (Exception exception)
            {
                _valuesLoadError = "Values.json could not be read: " + exception.Message;
                _error = _valuesLoadError;
                return new List<PresetValue>();
            }
        }

        private void RememberValuesOnDisk(IReadOnlyList<PresetValue> values = null)
        {
            _valuesFileSignature = Repository.ValuesSignature();
            _valuesSnapshot = PresetRepository.SerializeValues(values ?? Values);
        }

        /// <summary>
        /// Reads Values.json again when something else wrote it (e.g. a tool while in Play), so a later save keeps
        /// those Values. The list is refilled in place, since the open preset refers to it. Unsaved Value edits here
        /// are kept instead, with a warning that saving will overwrite the file.
        /// </summary>
        private void ReloadValuesWhenFileChanges()
        {
            if (_values == null) return;

            var signature = Repository.ValuesSignature();
            if (signature == _valuesFileSignature) return;

            if (_valuesLoadError == null && PresetRepository.SerializeValues(_values) != _valuesSnapshot)
            {
                _valuesFileSignature = signature;
                _valueError = "Values.json changed on disk, but Values here have unsaved edits. Saving overwrites it.";
                return;
            }

            GuardValues(() =>
            {
                var loaded = Repository.LoadValues();
                _values.Clear();
                _values.AddRange(loaded);
                RememberValuesOnDisk();
                if (_valuesLoadError != null && _error == _valuesLoadError) _error = null;
                _valuesLoadError = null;
                _valuesRevision++;
                QueuePreview();
            });
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

        /// <summary>Like Guard, for Value operations: their message shows in the Values windows.</summary>
        private void GuardValues(Action action)
        {
            try
            {
                action();
                _valueError = null;
            }
            catch (Exception exception)
            {
                _valueError = exception.Message;
            }
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
            // The Value list is shared by every preset, so keep the selected Value across opening presets.
            if (FindValue(_selectedValueId) == null) _selectedValueId = Values.FirstOrDefault()?.Id;
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
