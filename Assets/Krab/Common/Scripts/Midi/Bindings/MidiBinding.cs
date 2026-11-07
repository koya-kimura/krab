using Krab.Common.Midi.Actions;
using Krab.Common.Midi.Core;
using PrefsGUI;
using UnityEngine;

namespace Krab.Common.Midi.Bindings
{
    public class MidiBinding
    {
        public MidiBinding(string paramId, IMidiAction action, PrefsColor ledColor)
        {
            ParamId = paramId;
            Action = action;
            LedColor = ledColor;
        }

        public string ParamId { get; }
        public MidiControl? Control { get; set; }
        public IMidiAction Action { get; }
        public PrefsColor LedColor { get; }
        public Color ResolveLedColor() => Action.ResolveLedColor(LedColor.Get());
    }
}
