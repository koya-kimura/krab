using System;
using System.Collections.Generic;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class StateMidiAction : IMidiAction
    {
        private readonly string _actionId;
        private readonly Param<int> _param;
        private readonly int _stateCount;
        private readonly Color[] _stateColors;
        private readonly MidiActionStateRepository _repository;
        private bool _normalizing;

        public StateMidiAction(string actionId, Param<int> param, int stateCount,
            IReadOnlyList<Color> stateColors, MidiActionStateRepository repository)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));

            _param = param ?? throw new ArgumentNullException(nameof(param));
            if (stateCount < 2) throw new ArgumentOutOfRangeException(nameof(stateCount));

            if (stateColors != null && stateColors.Count != stateCount)
                throw new ArgumentException("State color count must match state count.", nameof(stateColors));

            _actionId = actionId;
            _stateCount = stateCount;
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (stateColors != null)
            {
                _stateColors = new Color[stateColors.Count];
                for (var i = 0; i < stateColors.Count; i++) _stateColors[i] = stateColors[i];
            }

            var loaded = Normalize(_repository.LoadStateIndex(_actionId, _param.Value));
            _param.SetValue(loaded);
            _repository.SaveStateIndex(_actionId, loaded);
            _param.Changed += OnValueChanged;
        }

        public int State => _param.Value;
        public MidiActionKind Kind => MidiActionKind.State;
        public string DisplayName => $"State ({_stateCount} states)";
        public IMidiActionRuntime Runtime => null;

        public void Execute(MidiMessage message)
        {
            if (message.IsPressed) _param.SetValue((_param.Value + 1) % _stateCount);
        }

        public Color ResolveLedColor(Color configuredColor)
        {
            if (_stateColors != null) return _stateColors[_param.Value];

            return _param.Value == 0 ? MidiLedColor.Dim(configuredColor) : configuredColor;
        }

        public void Dispose()
        {
            _param.Changed -= OnValueChanged;
        }

        private int Normalize(int value)
        {
            return value >= 0 && value < _stateCount ? value : 0;
        }

        private void OnValueChanged(int value)
        {
            if (_normalizing) return;

            var normalized = Normalize(value);
            if (normalized != value)
            {
                _normalizing = true;
                _param.SetValue(normalized);
                _normalizing = false;
            }

            _repository.SaveStateIndex(_actionId, normalized);
        }
    }
}
