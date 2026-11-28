using System;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class ToggleMidiAction : IMidiAction
    {
        private readonly string _actionId;
        private readonly Param<bool> _param;
        private readonly MidiActionStateRepository _repository;

        public ToggleMidiAction(Param<bool> param)
            : this(null, param, null)
        {
        }

        public ToggleMidiAction(string actionId, Param<bool> param, MidiActionStateRepository repository)
        {
            _param = param ?? throw new ArgumentNullException(nameof(param));
            _actionId = actionId;
            _repository = repository;

            if (_repository == null) return;

            _param.SetValue(_repository.LoadBool(_actionId, _param.Value));
            _param.Changed += OnValueChanged;
        }

        public bool IsActive => _param.Value;
        public MidiActionKind Kind => MidiActionKind.Toggle;
        public string DisplayName => "Toggle";
        public IMidiActionRuntime Runtime => null;

        public void Execute(MidiMessage message)
        {
            if (!message.IsPressed) return;

            _param.SetValue(!_param.Value);
        }

        public Color ResolveLedColor(Color configuredColor)
        {
            return _param.Value ? configuredColor : MidiLedColor.Dim(configuredColor);
        }

        public void Dispose()
        {
            if (_repository != null) _param.Changed -= OnValueChanged;
        }

        private void OnValueChanged(bool value)
        {
            _repository.SaveBool(_actionId, value);
        }
    }
}
