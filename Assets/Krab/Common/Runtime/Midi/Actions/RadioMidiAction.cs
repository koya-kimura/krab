using System;
using System.Collections.Generic;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class RadioMidiAction : IDisposable
    {
        private readonly string _actionId;
        private readonly Param<int> _selectedIndex;
        private readonly MidiActionStateRepository _repository;
        private readonly List<string> _optionKeys = new();
        private bool _normalizing;

        public RadioMidiAction(string actionId, Param<int> selectedIndex, int optionCount,
            MidiActionStateRepository repository)
            : this(actionId, selectedIndex, CreateIndexKeys(optionCount), repository)
        {
        }

        public RadioMidiAction(string actionId, Param<int> selectedIndex, IReadOnlyList<string> optionKeys,
            MidiActionStateRepository repository)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));

            _selectedIndex = selectedIndex ?? throw new ArgumentNullException(nameof(selectedIndex));
            ValidateOptionKeys(optionKeys);
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _actionId = actionId;
            _optionKeys.AddRange(optionKeys);

            var loaded = Normalize(_repository.LoadRadioIndex(_actionId, _selectedIndex.Value));
            _selectedIndex.SetValue(loaded);
            _repository.SaveRadioIndex(_actionId, loaded);
            _selectedIndex.Changed += OnValueChanged;
        }

        public int OptionCount => _optionKeys.Count;
        public int SelectedIndex => _selectedIndex.Value;

        public string GetBindingId(int index)
        {
            return GetBindingIdForKey(_optionKeys[Validate(index)]);
        }

        public string GetBindingIdForKey(string optionKey)
        {
            return $"{_actionId}.Option.{ValidateNonEmptyKey(optionKey)}";
        }

        public void Dispose()
        {
            _selectedIndex.Changed -= OnValueChanged;
        }

        internal IMidiAction CreateOption(string optionKey)
        {
            return new RadioOptionMidiAction(this, ValidateKey(optionKey));
        }

        internal void SetOptionKeys(IReadOnlyList<string> optionKeys)
        {
            ValidateOptionKeys(optionKeys);
            var oldIndex = SelectedIndex;
            var selectedKey = oldIndex >= 0 && oldIndex < _optionKeys.Count ? _optionKeys[oldIndex] : null;
            _optionKeys.Clear();
            _optionKeys.AddRange(optionKeys);

            var newIndex = selectedKey == null ? -1 : _optionKeys.IndexOf(selectedKey);
            if (newIndex < 0) newIndex = Math.Min(Math.Max(oldIndex, 0), _optionKeys.Count - 1);

            _selectedIndex.SetValue(newIndex);
        }

        private static string ValidateNonEmptyKey(string optionKey)
        {
            if (string.IsNullOrWhiteSpace(optionKey))
                throw new ArgumentException("A radio option key is required.", nameof(optionKey));

            return optionKey;
        }

        private static void ValidateOptionKeys(IReadOnlyList<string> optionKeys)
        {
            if (optionKeys == null || optionKeys.Count < 1)
                throw new ArgumentException("A radio group requires at least one option.", nameof(optionKeys));

            var unique = new HashSet<string>();
            foreach (var key in optionKeys)
            {
                if (string.IsNullOrWhiteSpace(key) || !unique.Add(key))
                    throw new ArgumentException("Radio option keys must be non-empty and unique.", nameof(optionKeys));
            }
        }

        private static string[] CreateIndexKeys(int optionCount)
        {
            if (optionCount < 1) throw new ArgumentOutOfRangeException(nameof(optionCount));

            var keys = new string[optionCount];
            for (var i = 0; i < keys.Length; i++) keys[i] = i.ToString();
            return keys;
        }

        private int Validate(int index)
        {
            if (index < 0 || index >= OptionCount) throw new ArgumentOutOfRangeException(nameof(index));

            return index;
        }

        private int Normalize(int index)
        {
            return index >= 0 && index < OptionCount ? index : 0;
        }

        private string ValidateKey(string optionKey)
        {
            if (string.IsNullOrWhiteSpace(optionKey) || !_optionKeys.Contains(optionKey))
                throw new ArgumentException("The radio option key is not registered.", nameof(optionKey));

            return optionKey;
        }

        private void Select(int index)
        {
            _selectedIndex.SetValue(index);
        }

        private void OnValueChanged(int value)
        {
            if (_normalizing) return;

            var normalized = Normalize(value);
            if (normalized != value)
            {
                _normalizing = true;
                _selectedIndex.SetValue(normalized);
                _normalizing = false;
            }

            _repository.SaveRadioIndex(_actionId, normalized);
        }

        private sealed class RadioOptionMidiAction : IMidiAction
        {
            private readonly RadioMidiAction _owner;
            private readonly string _optionKey;

            public RadioOptionMidiAction(RadioMidiAction owner, string optionKey)
            {
                _owner = owner;
                _optionKey = optionKey;
            }

            public MidiActionKind Kind => MidiActionKind.Radio;
            public string DisplayName => $"Radio (option {Index + 1}/{_owner.OptionCount})";
            public IMidiActionRuntime Runtime => null;

            private int Index => _owner._optionKeys.IndexOf(_optionKey);

            public void Execute(MidiMessage message)
            {
                if (message.IsPressed && Index >= 0) _owner.Select(Index);
            }

            public Color ResolveLedColor(Color configuredColor)
            {
                return _owner.SelectedIndex == Index ? configuredColor : MidiLedColor.Dim(configuredColor);
            }

            public void Dispose()
            {
            }
        }
    }
}
