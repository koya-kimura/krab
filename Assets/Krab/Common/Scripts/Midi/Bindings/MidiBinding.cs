using System;
using Krab.Common.Midi.Actions;
using Krab.Common.Midi.Core;
using PrefsGUI;
using UnityEngine;

namespace Krab.Common.Midi.Bindings
{
    /// <summary>An action with at most one control per device, so each controller can drive it.</summary>
    public sealed class MidiBinding
    {
        private static readonly int DeviceCount = Enum.GetValues(typeof(MidiDeviceKind)).Length;

        private readonly MidiControl?[] _controls = new MidiControl?[DeviceCount];

        public MidiBinding(string paramId, IMidiAction action, PrefsColor ledColor)
        {
            ParamId = paramId;
            Action = action;
            LedColor = ledColor;
        }

        public string ParamId { get; }
        public IMidiAction Action { get; }
        public PrefsColor LedColor { get; }

        public bool HasControl
        {
            get
            {
                foreach (var control in _controls)
                {
                    if (control.HasValue) return true;
                }

                return false;
            }
        }

        public MidiControl? GetControl(MidiDeviceKind device)
        {
            return _controls[(int)device];
        }

        internal void SetControl(MidiDeviceKind device, MidiControl? control)
        {
            if (control.HasValue && control.Value.Device != device)
                throw new ArgumentException("The control belongs to another device.", nameof(control));

            _controls[(int)device] = control;
        }

        public bool Matches(MidiControl control)
        {
            return _controls[(int)control.Device] == control;
        }

        public Color ResolveLedColor()
        {
            return Action.ResolveLedColor(LedColor.Get());
        }
    }
}
