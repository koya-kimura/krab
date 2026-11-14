using System;
using Krab.Common.Midi.Core;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    /// <summary>APC mini mk2 input numbers used by the application.</summary>
    public static class ApcMiniMk2
    {
        public const int GridSize = 8;
        public const int PageCount = 8;
        public const int GlobalPage = -1;
        public const int PadFirst = 0x00;
        public const int PadLast = 0x3f;
        public const int TrackButtonFirst = 0x64;
        public const int TrackButtonLast = 0x6b;
        public const int PageButtonFirst = 0x70;
        public const int PageButtonLast = 0x77;
        public const int ShiftButton = 0x7a;
        public const int FaderCcFirst = 0x30;
        public const int FaderCcLast = 0x38;
        public const int ChannelFaderCount = 8;
        public const int FaderCount = 9;
        public const int MasterFaderIndex = 8;

        public static int GetPadNote(int x, int y)
        {
            return PadFirst + y * GridSize + x;
        }

        /// <summary>
        /// Creates the logical key used by bindings. Page, x and y are all zero based;
        /// x grows left-to-right and y grows bottom-to-top.
        /// </summary>
        public static MidiControl CreatePadControl(int page, int x, int y)
        {
            if (page < 0 || page >= PageCount) throw new ArgumentOutOfRangeException(nameof(page));
            if (x < 0 || x >= GridSize) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= GridSize) throw new ArgumentOutOfRangeException(nameof(y));

            return new MidiControl(MidiMessageType.Note, page, GetPadNote(x, y));
        }

        public static bool TryGetPadPosition(int note, out int x, out int y)
        {
            if (note < PadFirst || note > PadLast)
            {
                x = -1;
                y = -1;
                return false;
            }

            var index = note - PadFirst;
            x = index % GridSize;
            y = index / GridSize;
            return true;
        }

        public static void ValidateFaderIndex(int index)
        {
            if (index < 0 || index >= FaderCount) throw new ArgumentOutOfRangeException(nameof(index));
        }

        public static void ValidateChannelFaderIndex(int index)
        {
            if (index < 0 || index >= ChannelFaderCount) throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
