using System;

namespace Krab.Common.Looping.Core
{
    /// <summary>
    /// Payload-independent controls of a looper bank, for UI and input adapters such as MIDI.
    /// </summary>
    public interface ILooperBank
    {
        public event Action<int> StateChanged;

        public int Count { get; }

        public EventLooperState GetState(int slot);

        public int GetEventCount(int slot);

        public double GetDuration(int slot);

        /// <summary>Idle → Recording → Playing → Idle, the single-button cycle of each slot.</summary>
        public void Cycle(int slot);

        public void Clear(int slot);
    }
}
