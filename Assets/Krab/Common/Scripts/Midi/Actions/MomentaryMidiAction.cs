using System;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    public sealed class MomentaryMidiAction : IMidiAction, IMidiActionRuntime, ITransientMidiAction
    {
        private readonly Param<bool> _param;
        private int _pressedFrame = -1;

        public MomentaryMidiAction(Param<bool> param)
        {
            _param = param ?? throw new ArgumentNullException(nameof(param));
            _param.SetValue(false);
        }

        public bool IsActive => _param.Value;
        public MidiActionKind Kind => MidiActionKind.Momentary;
        public string DisplayName => "Momentary (one frame)";

        public void Execute(MidiMessage message)
        {
            if (!message.IsPressed) return;
            _pressedFrame = Time.frameCount;
            _param.SetValue(true);
        }

        public void Update(in MidiActionUpdateContext context)
        {
            if (_param.Value && context.FrameCount > _pressedFrame)
                _param.SetValue(false);
        }

        public Color ResolveLedColor(Color configuredColor) =>
            _param.Value ? configuredColor : MidiLedColor.Dim(configuredColor);

        public IMidiActionRuntime Runtime => this;

        public void ResetTransientState()
        {
            _pressedFrame = -1;
            _param.SetValue(false);
        }

        public void Dispose() { }
    }
}
