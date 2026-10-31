using System;
using Krab.Common.Midi.Core;
using UnityEngine;

namespace Krab.Common.Midi.Actions
{
    /// <summary>Writes a normalized MIDI CC value (0-1) to a float parameter.</summary>
    public sealed class FloatMidiAction : IMidiAction
    {
        private readonly Param<float> _param;

        public FloatMidiAction(Param<float> param)
        {
            _param = param ?? throw new ArgumentNullException(nameof(param));
        }

        public MidiActionKind Kind => MidiActionKind.Float;
        public string DisplayName => "Float";

        public void Execute(MidiMessage message)
        {
            if (message.Control.Type != MidiMessageType.ControlChange)
                return;

            _param.SetValue(Math.Clamp(message.Value, 0f, 1f));
        }

        public Color ResolveLedColor(Color configuredColor) => configuredColor;
        public IMidiActionRuntime Runtime => null;
        public void Dispose() { }
    }
}
