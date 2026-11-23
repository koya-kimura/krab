using System;
using System.Collections.Generic;
using Krab.Common.Midi.Actions;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;
using PrefsGUI;

namespace Krab.ImageLayer.Performance
{
    /// <summary>
    /// One drawing slot with two radio groups: which layout preset and which image entry. Options are keyed by
    /// preset and entry ids, so MIDI bindings survive renames, reordering and additions. MIDI is optional.
    /// </summary>
    public sealed class ImageDeck : IDisposable
    {
        public const string OffKey = "off";
        public const string NoImageKey = "none";

        private readonly PrefsString _layoutKey;
        private readonly PrefsString _imageKey;
        private readonly Param<int> _layoutIndex = new(0);
        private readonly Param<int> _imageIndex = new(0);
        private readonly List<string> _layoutKeys = new() { OffKey };
        private readonly List<string> _imageKeys = new() { NoImageKey };

        private MidiBindingManager _midi;
        private RadioMidiAction _layoutRadio;
        private RadioMidiAction _imageRadio;
        private bool _syncing;

        public ImageDeck(string name)
        {
            Name = name;
            _layoutKey = new PrefsString($"ImageLayer Deck {name} Layout", OffKey);
            _imageKey = new PrefsString($"ImageLayer Deck {name} Image", NoImageKey);
            _layoutIndex.Changed += index => OnIndexChanged(index, _layoutKeys, _layoutKey);
            _imageIndex.Changed += index => OnIndexChanged(index, _imageKeys, _imageKey);
        }

        public string Name { get; }

        /// <summary>Selected preset id, or <see cref="OffKey"/>. A removed preset reads as Off.</summary>
        public string LayoutKey => Resolve(_layoutKeys, _layoutKey.Get());

        /// <summary>Selected entry id; a removed entry reads as the first one.</summary>
        public string ImageKey => Resolve(_imageKeys, _imageKey.Get());

        /// <summary>Why the last draw was skipped, or null.</summary>
        public string Error { get; set; }

        private string LayoutActionId => $"ImageLayer.Deck {Name}.Layout";

        private string ImageActionId => $"ImageLayer.Deck {Name}.Image";

        public void SelectLayout(string key) => Select(key, _layoutKeys, _layoutKey, _layoutIndex);

        public void SelectImage(string key) => Select(key, _imageKeys, _imageKey, _imageIndex);

        public string LayoutBindingId(string key) => BindingId(LayoutActionId, key);

        public string ImageBindingId(string key) => BindingId(ImageActionId, key);

        /// <summary>Replaces the radio options after presets or entries were added, removed or reordered.</summary>
        public void SetOptions(IEnumerable<string> layoutIds, IEnumerable<string> imageIds)
        {
            _layoutKeys.Clear();
            _layoutKeys.Add(OffKey);
            _layoutKeys.AddRange(layoutIds);

            _imageKeys.Clear();
            _imageKeys.AddRange(imageIds);
            if (_imageKeys.Count == 0) _imageKeys.Add(NoImageKey);

            _syncing = true;
            try
            {
                _layoutRadio = UpdateRadio(_layoutRadio, LayoutActionId, _layoutIndex, _layoutKeys);
                _imageRadio = UpdateRadio(_imageRadio, ImageActionId, _imageIndex, _imageKeys);

                // Prefs hold the selection; the radio's own saved index may point elsewhere after reordering.
                _layoutIndex.SetValue(_layoutKeys.IndexOf(LayoutKey));
                _imageIndex.SetValue(_imageKeys.IndexOf(ImageKey));
            }
            finally
            {
                _syncing = false;
            }
        }

        public void AttachMidi(MidiBindingManager midi)
        {
            if (midi == null || _midi != null) return;

            _midi = midi;
            SetOptions(_layoutKeys.GetRange(1, _layoutKeys.Count - 1), ImageIdsWithoutPlaceholder());
        }

        public void Dispose()
        {
            if (_midi == null) return;

            _midi.UnregisterAction(LayoutActionId);
            _midi.UnregisterAction(ImageActionId);
            _layoutRadio = null;
            _imageRadio = null;
            _midi = null;
        }

        private static string Resolve(List<string> keys, string stored) => keys.Contains(stored) ? stored : keys[0];

        private static string BindingId(string actionId, string key) => $"{actionId}.Option.{key}";

        private void Select(string key, List<string> keys, PrefsString stored, Param<int> index)
        {
            if (!keys.Contains(key)) return;

            stored.Set(key);
            index.SetValue(keys.IndexOf(key));
        }

        private void OnIndexChanged(int index, List<string> keys, PrefsString stored)
        {
            if (_syncing || index < 0 || index >= keys.Count) return;

            stored.Set(keys[index]);
        }

        private RadioMidiAction UpdateRadio(RadioMidiAction radio, string actionId, Param<int> index,
            List<string> keys)
        {
            if (_midi == null) return null;

            // Copy: the action keeps the list it is given.
            var optionKeys = keys.ToArray();
            if (radio == null) return _midi.RegisterRadio(actionId, index, optionKeys);

            _midi.SetRadioOptions(actionId, optionKeys);
            return radio;
        }

        private List<string> ImageIdsWithoutPlaceholder()
        {
            var placeholderOnly = _imageKeys.Count == 1 && _imageKeys[0] == NoImageKey;
            return placeholderOnly ? new List<string>() : new List<string>(_imageKeys);
        }
    }
}
