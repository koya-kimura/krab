using System;
using System.Collections.Generic;

namespace Krab.Common.Looping.Core
{
    /// <summary>
    /// A fixed set of independent loopers sharing one clock. Manual events go to every recording slot;
    /// the owner plays back each slot's events and decides what they trigger.
    /// </summary>
    public sealed class LooperBank<T> : ILooperBank
    {
        private readonly EventLooper<T>[] _loopers;
        private readonly Func<double> _clock;

        public LooperBank(int count, Func<double> clock)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _loopers = new EventLooper<T>[count];
            for (var slot = 0; slot < count; slot++) _loopers[slot] = new EventLooper<T>();
        }

        public event Action<int> StateChanged;

        public int Count => _loopers.Length;

        public EventLooper<T> this[int slot] => _loopers[RequireSlot(slot)];

        public EventLooperState GetState(int slot) => this[slot].State;

        public int GetEventCount(int slot) => this[slot].EventCount;

        public double GetDuration(int slot) => this[slot].Duration;

        public void Cycle(int slot)
        {
            var looper = this[slot];
            switch (looper.State)
            {
                case EventLooperState.Idle:
                    looper.StartRecording(_clock());
                    break;
                case EventLooperState.Recording:
                    // An empty or zero-length recording returns to Idle instead of playing.
                    looper.FinishRecording(_clock());
                    break;
                default:
                    looper.Clear();
                    break;
            }

            StateChanged?.Invoke(slot);
        }

        public void Clear(int slot)
        {
            var looper = this[slot];
            if (looper.State == EventLooperState.Idle && looper.EventCount == 0) return;

            looper.Clear();
            StateChanged?.Invoke(slot);
        }

        public void ClearAll()
        {
            for (var slot = 0; slot < _loopers.Length; slot++) Clear(slot);
        }

        /// <summary>Records into every recording slot. Returns the number of slots that recorded it.</summary>
        public int Record(T payload)
        {
            var position = _clock();
            var recorded = 0;
            foreach (var looper in _loopers)
            {
                if (looper.Record(payload, position)) recorded++;
            }

            return recorded;
        }

        /// <summary>Appends the slot's events due by now. See <see cref="EventLooper{T}.Advance"/>.</summary>
        public int Advance(int slot, List<LoopPlaybackEvent<T>> output)
        {
            return this[slot].Advance(_clock(), output);
        }

        private int RequireSlot(int slot)
        {
            if (slot < 0 || slot >= _loopers.Length) throw new ArgumentOutOfRangeException(nameof(slot));

            return slot;
        }
    }
}
