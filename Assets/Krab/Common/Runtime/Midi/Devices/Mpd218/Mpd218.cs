using System;
using Krab.Common.Midi.Core;

namespace Krab.Common.Midi.Devices.Mpd218
{
    /// <summary>
    /// AKAI MPD218 numbers of the factory preset 1. Pad and knob banks are switched on the
    /// hardware, which changes the note / CC numbers it sends, so bindings need no page.
    /// </summary>
    public static class Mpd218
    {
        public const string ProductName = "MPD218";
        public const int PadGridSize = 4;
        public const int PadsPerBank = PadGridSize * PadGridSize;
        public const int PadBankCount = 3;
        public const int PadNoteFirst = 36;
        public const int PadNoteLast = PadNoteFirst + PadsPerBank * PadBankCount - 1;
        public const int KnobsPerBank = 6;
        public const int KnobBankCount = 3;

        private static readonly int[] KnobCcs =
        {
            3, 9, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21,
            22, 23, 24, 25, 26, 27
        };

        public static bool IsProduct(string productName)
        {
            return !string.IsNullOrEmpty(productName) &&
                   productName.IndexOf(ProductName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Index 0 is the bottom-left pad (labelled 1 / 17 / 33); it grows left-to-right, then up.</summary>
        public static int GetPadNote(int bank, int index)
        {
            if (bank < 0 || bank >= PadBankCount) throw new ArgumentOutOfRangeException(nameof(bank));
            if (index < 0 || index >= PadsPerBank) throw new ArgumentOutOfRangeException(nameof(index));

            return PadNoteFirst + bank * PadsPerBank + index;
        }

        public static bool TryGetPad(int note, out int bank, out int index)
        {
            if (note < PadNoteFirst || note > PadNoteLast)
            {
                bank = -1;
                index = -1;
                return false;
            }

            var offset = note - PadNoteFirst;
            bank = offset / PadsPerBank;
            index = offset % PadsPerBank;
            return true;
        }

        public static int GetKnobCc(int bank, int index)
        {
            if (bank < 0 || bank >= KnobBankCount) throw new ArgumentOutOfRangeException(nameof(bank));
            if (index < 0 || index >= KnobsPerBank) throw new ArgumentOutOfRangeException(nameof(index));

            return KnobCcs[bank * KnobsPerBank + index];
        }

        public static bool TryGetKnob(int cc, out int bank, out int index)
        {
            var position = Array.IndexOf(KnobCcs, cc);
            bank = position < 0 ? -1 : position / KnobsPerBank;
            index = position < 0 ? -1 : position % KnobsPerBank;
            return position >= 0;
        }

        public static string GetPadBankName(int bank)
        {
            return ((char)('A' + bank)).ToString();
        }

        public static MidiControl CreateNoteControl(int note)
        {
            return new MidiControl(MidiMessageType.Note, 0, note, MidiDeviceKind.Mpd218);
        }

        public static MidiControl CreateCcControl(int cc)
        {
            return new MidiControl(MidiMessageType.ControlChange, 0, cc, MidiDeviceKind.Mpd218);
        }

        public static string Describe(MidiControl control)
        {
            if (control.Type == MidiMessageType.Note)
            {
                return TryGetPad(control.Number, out var bank, out var index)
                    ? $"Pad {GetPadBankName(bank)}{index + 1} (note {control.Number})"
                    : $"Note {control.Number}";
            }

            return TryGetKnob(control.Number, out var knobBank, out var knobIndex)
                ? $"Knob {knobBank + 1}-{knobIndex + 1} (CC {control.Number})"
                : $"CC {control.Number}";
        }
    }
}
