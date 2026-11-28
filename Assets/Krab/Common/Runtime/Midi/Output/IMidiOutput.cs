using System;

namespace Krab.Common.Midi.Output
{
    /// <summary>Raw MIDI output; device-specific LED interpretation belongs to its adapter.</summary>
    public interface IMidiOutput
    {
        bool IsConnected { get; }
        void SendNoteOn(int noteNumber, int velocity, int channel = 0);
        void SendNoteOff(int noteNumber, int channel = 0);
        void SendCc(int number, int value, int channel = 0);
        void SendRaw(ReadOnlySpan<byte> message);
    }
}
