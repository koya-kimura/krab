using System;

namespace Krab.Common.Midi.Core
{
    public enum MidiMessageType
    {
        Note,
        ControlChange
    }

    public readonly struct MidiControl : IEquatable<MidiControl>
    {
        public MidiMessageType Type { get; }
        public int Channel { get; }
        public int Number { get; }
        public MidiDeviceKind Device { get; }

        /// <summary>
        /// MIDI channel, or a device adapter's logical page when the hardware does not
        /// expose independently addressable pages. APC mini mk2 uses this as its page.
        /// </summary>
        public int Page => Channel;

        public MidiControl(MidiMessageType type, int channel, int number,
            MidiDeviceKind device = MidiDeviceKind.ApcMiniMk2)
        {
            Type = type;
            Channel = channel;
            Number = number;
            Device = device;
        }

        public bool Equals(MidiControl other)
        {
            return Type == other.Type && Channel == other.Channel && Number == other.Number &&
                   Device == other.Device;
        }

        public override bool Equals(object obj)
        {
            return obj is MidiControl other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Type;
                hash = (hash * 397) ^ Channel;
                hash = (hash * 397) ^ Number;
                hash = (hash * 397) ^ (int)Device;
                return hash;
            }
        }

        public static bool operator ==(MidiControl left, MidiControl right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MidiControl left, MidiControl right)
        {
            return !left.Equals(right);
        }
    }
}
