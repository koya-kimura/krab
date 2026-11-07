using UnityEngine;

namespace Krab.Common.Midi.Devices.ApcMiniMk2
{
    /// <summary>The shared input and LED state used by the hardware and its on-screen simulator.</summary>
    public interface IApcMiniMk2Surface
    {
        ApcMiniMk2InputAdapter Adapter { get; }
        ApcMiniMk2FaderModeController FaderModes { get; }
        Color GetPadColor(int note);
        bool GetTrackLed(int index);
        Color GetShiftColor();
    }
}
