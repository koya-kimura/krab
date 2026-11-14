using System;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class OneShotMidiAction : IMidiAction, ITransientMidiAction
    {
        private readonly Param<bool> _param;

        public OneShotMidiAction(Param<bool> param)
        {
            _param = param ?? throw new ArgumentNullException(nameof(param));
            _param.SetValue(false);
        }

        public bool IsActive => _param.Value;
        public MidiActionKind Kind => MidiActionKind.OneShot;
        public string DisplayName => "OneShot (held)";
        public IMidiActionRuntime Runtime => null;

        public void Execute(MidiMessage message)
        {
            if (message.IsPressed) _param.SetValue(true);
            else if (message.IsReleased) _param.SetValue(false);
        }

        public Color ResolveLedColor(Color configuredColor)
        {
            return _param.Value ? configuredColor : MidiLedColor.Dim(configuredColor);
        }

        public void ResetTransientState()
        {
            _param.SetValue(false);
        }

        public void Dispose()
        {
        }
    }
}
