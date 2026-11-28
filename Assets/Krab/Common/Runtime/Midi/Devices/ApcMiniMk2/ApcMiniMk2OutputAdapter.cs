using System;
using Krab.Common.Midi.Output;
using UnityEngine;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    /// <summary>APC mini mk2 LED protocol. Logical pages never become hardware MIDI channels.</summary>
    public sealed class ApcMiniMk2OutputAdapter
    {
        private readonly IMidiOutput _output;

        public ApcMiniMk2OutputAdapter(IMidiOutput output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public void SetPad(int note, Color color)
        {
            if (note < ApcMiniMk2.PadFirst || note > ApcMiniMk2.PadLast) return;

            SetPadRange(note, note, color);
        }

        public void SetPadRange(int firstNote, int lastNote, Color color)
        {
            if (firstNote < ApcMiniMk2.PadFirst || lastNote > ApcMiniMk2.PadLast || firstNote > lastNote) return;

            // APC mini mk2 SysEx RGB message: each 8-bit component is split into
            // two 7-bit MIDI data bytes. See the reference protocol helper.
            var r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            var g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            var b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            Span<byte> message = stackalloc byte[16];
            message[0] = 0xf0;
            message[1] = 0x47;
            message[2] = 0x7f;
            message[3] = 0x4f;
            message[4] = 0x24;
            message[5] = 0x00;
            message[6] = 0x08;
            message[7] = (byte)firstNote;
            message[8] = (byte)lastNote;
            message[9] = (byte)(r >> 7);
            message[10] = (byte)(r & 0x7f);
            message[11] = (byte)(g >> 7);
            message[12] = (byte)(g & 0x7f);
            message[13] = (byte)(b >> 7);
            message[14] = (byte)(b & 0x7f);
            message[15] = 0xf7;
            _output.SendRaw(message);
        }

        public void SetButton(int note, bool lit)
        {
            var isTrack = note >= ApcMiniMk2.TrackButtonFirst && note <= ApcMiniMk2.TrackButtonLast;
            var isPage = note >= ApcMiniMk2.PageButtonFirst && note <= ApcMiniMk2.PageButtonLast;
            if (!isTrack && !isPage) return;

            _output.SendNoteOn(note, lit ? 1 : 0);
        }
    }
}
