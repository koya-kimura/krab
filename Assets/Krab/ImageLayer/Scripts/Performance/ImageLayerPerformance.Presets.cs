using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Krab.ImageLayer.Core.Presets;

namespace Krab.ImageLayer.Performance
{
    /// <summary>Layout presets and image entries: editing in memory, saving to and reverting from JSON.</summary>
    public sealed partial class ImageLayerPerformance
    {
        private readonly ImageLayerPresetStore _store = new();
        private readonly List<LayoutPreset> _layouts = new();
        private readonly List<ImageEntry> _imageEntries = new();

        private string _imagesLibrary;
        private string _layoutsStatus = string.Empty;
        private string _imagesStatus = string.Empty;
        private bool _layoutsDirty;
        private bool _imagesDirty;

        /// <summary>Changes when presets or entries are added, removed, reordered or reloaded (not on edits).</summary>
        private int _structureVersion;

        public IReadOnlyList<LayoutPreset> Layouts => _layouts;

        public IReadOnlyList<ImageEntry> ImageEntries => _imageEntries;

        public IReadOnlyList<ImagePattern> Patterns => _patterns;

        public void SaveLayouts()
        {
            if (TrySave(() => _store.SaveLayouts(_layouts), ref _layoutsStatus)) _layoutsDirty = false;
        }

        public void SaveImages()
        {
            if (_imagesLibrary == null) return;

            if (TrySave(() => _store.SaveImages(_imagesLibrary, _imageEntries), ref _imagesStatus))
                _imagesDirty = false;
        }

        public LayoutPreset FindLayout(string id) => _layouts.Find(preset => preset.Id == id);

        public ImageEntry FindImage(string id) => _imageEntries.Find(entry => entry.Id == id);

        public ImagePattern FindPattern(string name) => Array.Find(_patterns, pattern => pattern.Name == name);

        private void LoadLayouts()
        {
            _layouts.Clear();
            _layoutsDirty = false;
            if (_store.TryLoadLayouts(out var loaded, out var error))
            {
                _layouts.AddRange(loaded);
                _layoutsStatus = error ?? string.Empty;
            }
            else
            {
                // First run, or an unreadable file: start from one preset per pattern and leave the file alone.
                foreach (var pattern in _patterns)
                    AddLayoutPreset(pattern);

                _layoutsStatus = error ?? "Created one preset per pattern (not saved yet).";
            }

            OnStructureChanged();
        }

        private void LoadImages()
        {
            _imageEntries.Clear();
            _imagesDirty = false;
            _imagesLibrary = _library?.Name;
            if (_imagesLibrary == null)
            {
                OnStructureChanged();
                return;
            }

            if (_store.TryLoadImages(_imagesLibrary, out var loaded, out var error))
            {
                _imageEntries.AddRange(loaded);
                _imagesStatus = error ?? string.Empty;
            }
            else
            {
                AddMissingSetEntries();
                _imagesStatus = error ?? "Created one entry per set (not saved yet).";
            }

            OnStructureChanged();
        }

        private void AddLayout(string patternName)
        {
            var pattern = FindPattern(patternName);
            if (pattern == null) return;

            AddLayoutPreset(pattern);
            MarkLayoutsChanged(true);
        }

        private void AddLayoutPreset(ImagePattern pattern)
        {
            var id = PresetNaming.CreateId(pattern.Name, _layouts.Select(preset => preset.Id).ToList());
            var name = PresetNaming.CreateName(pattern.Name, _layouts.Select(preset => preset.Name).ToList());
            _layouts.Add(new LayoutPreset(id, name, pattern.Name, ParameterValues.Defaults(pattern.Parameters)));
        }

        private void DuplicateLayout(LayoutPreset preset)
        {
            var id = PresetNaming.CreateId(preset.Name, _layouts.Select(other => other.Id).ToList());
            var name = PresetNaming.CreateName(preset.Name, _layouts.Select(other => other.Name).ToList());
            _layouts.Insert(_layouts.IndexOf(preset) + 1, preset.Duplicate(id, name));
            MarkLayoutsChanged(true);
        }

        private void DeleteLayout(LayoutPreset preset)
        {
            if (_layouts.Remove(preset)) MarkLayoutsChanged(true);
        }

        private void MoveLayout(LayoutPreset preset, int offset)
        {
            if (Move(_layouts, preset, offset)) MarkLayoutsChanged(true);
        }

        private void AddImage()
        {
            if (_setNames.Count == 0) return;

            AddImageEntry(_setNames[0], ImageEntry.AllClips);
            MarkImagesChanged(true);
        }

        private void AddImageEntry(string setName, int clip)
        {
            var baseName = clip == ImageEntry.AllClips ? setName : $"{setName} {clip:00}";
            var id = PresetNaming.CreateId(baseName, _imageEntries.Select(entry => entry.Id).ToList());
            var name = PresetNaming.CreateName(baseName, _imageEntries.Select(entry => entry.Name).ToList());
            _imageEntries.Add(new ImageEntry(id, name, setName, clip, nameof(ImageLook.Original)));
        }

        /// <summary>One all-clips entry for every loaded set that has none yet.</summary>
        private void AddMissingSetEntries()
        {
            foreach (var setName in _setNames)
            {
                if (_imageEntries.Exists(entry => entry.SetName == setName && entry.Clip == ImageEntry.AllClips))
                    continue;

                AddImageEntry(setName, ImageEntry.AllClips);
            }
        }

        private void AddMissingImages()
        {
            var count = _imageEntries.Count;
            AddMissingSetEntries();
            if (_imageEntries.Count != count) MarkImagesChanged(true);
        }

        private void DuplicateImage(ImageEntry entry)
        {
            var id = PresetNaming.CreateId(entry.Name, _imageEntries.Select(other => other.Id).ToList());
            var name = PresetNaming.CreateName(entry.Name, _imageEntries.Select(other => other.Name).ToList());
            _imageEntries.Insert(_imageEntries.IndexOf(entry) + 1, entry.Duplicate(id, name));
            MarkImagesChanged(true);
        }

        private void DeleteImage(ImageEntry entry)
        {
            if (_imageEntries.Remove(entry)) MarkImagesChanged(true);
        }

        private void MoveImage(ImageEntry entry, int offset)
        {
            if (Move(_imageEntries, entry, offset)) MarkImagesChanged(true);
        }

        private void MarkLayoutsChanged(bool structure = false)
        {
            _layoutsDirty = true;
            if (structure) OnStructureChanged();
        }

        private void MarkImagesChanged(bool structure = false)
        {
            _imagesDirty = true;
            if (structure) OnStructureChanged();
        }

        private void OnStructureChanged()
        {
            _structureVersion++;
            var layoutIds = _layouts.Select(preset => preset.Id).ToList();
            var imageIds = _imageEntries.Select(entry => entry.Id).ToList();
            foreach (var deck in _decks)
                deck.SetOptions(layoutIds, imageIds);
        }

        private static bool Move<T>(List<T> list, T item, int offset)
        {
            var index = list.IndexOf(item);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= list.Count) return false;

            list.RemoveAt(index);
            list.Insert(target, item);
            return true;
        }

        private static bool TrySave(Action save, ref string status)
        {
            try
            {
                save();
                status = $"Saved {DateTime.Now:HH:mm:ss}.";
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                status = $"Save failed: {exception.Message}";
                return false;
            }
        }
    }
}
