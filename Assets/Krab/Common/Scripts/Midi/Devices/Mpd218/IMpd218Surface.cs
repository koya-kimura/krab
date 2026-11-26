using UnityEngine;

namespace Krab.Common.Midi.Devices.Mpd218
{
    /// <summary>The shared MPD218 input and on-screen pad colors used by the hardware and its simulator.</summary>
    public interface IMpd218Surface
    {
        public Mpd218InputAdapter Adapter { get; }
        public Color GetPadColor(int note);
    }
}
