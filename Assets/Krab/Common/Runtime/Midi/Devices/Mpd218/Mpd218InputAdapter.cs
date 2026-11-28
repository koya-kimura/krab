using System;
using Krab.Common.Midi.Bindings;
using Krab.Common.Midi.Core;

namespace Krab.Common.Midi.Devices.Mpd218
{
    /// <summary>
    /// Forwards every MPD218 note and CC, so presets edited on the hardware can still be learned.
    /// Pad velocity is passed as the message value.
    /// </summary>
    public sealed class Mpd218InputAdapter
    {
        private const int MidiDataCount = 128;

        private readonly MidiBindingManager _bindingManager;
        private readonly float[] _ccValues = new float[MidiDataCount];
        private readonly bool[] _pressedNotes = new bool[MidiDataCount];

        public Mpd218InputAdapter(MidiBindingManager bindingManager)
        {
            _bindingManager = bindingManager ?? throw new ArgumentNullException(nameof(bindingManager));
        }

        public float GetCcValue(int cc)
        {
            return IsDataByte(cc) ? _ccValues[cc] : 0f;
        }

        public bool IsNotePressed(int note)
        {
            return IsDataByte(note) && _pressedNotes[note];
        }

        public void OnNoteOn(int note, float velocity)
        {
            if (!IsDataByte(note)) return;

            _pressedNotes[note] = true;
            _bindingManager.Receive(new MidiMessage(Mpd218.CreateNoteControl(note), velocity, true, false));
        }

        public void OnNoteOff(int note)
        {
            if (!IsDataByte(note)) return;

            _pressedNotes[note] = false;
            _bindingManager.Receive(new MidiMessage(Mpd218.CreateNoteControl(note), 0f, false, true));
        }

        public void OnControlChange(int cc, float value)
        {
            if (!IsDataByte(cc)) return;

            _ccValues[cc] = Math.Clamp(value, 0f, 1f);
            _bindingManager.Receive(new MidiMessage(Mpd218.CreateCcControl(cc), _ccValues[cc], false, false));
        }

        /// <summary>Forgets held pads when the device disconnects; bindings release their own state.</summary>
        public void ReleaseAllNotes()
        {
            Array.Clear(_pressedNotes, 0, _pressedNotes.Length);
        }

        private static bool IsDataByte(int value)
        {
            return value is >= 0 and < MidiDataCount;
        }
    }
}
