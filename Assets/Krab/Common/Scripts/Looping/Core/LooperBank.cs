using System;
using System.Collections.Generic;

namespace Krab.Common.Looping.Core
{
    /// <summary>
    /// A fixed set of independent loopers. Manual events go to every recording slot; the owner plays back
    /// each slot's events and decides what they trigger. Each slot reads its own clock, and a slot with a
    /// positive grid length snaps its loop to that grid when recording finishes (see LoopWindow.SnapToGrid).
    /// </summary>
    public sealed class LooperBank<T> : ILooperBank
    {
        private readonly EventLooper<T>[] _loopers;
        private readonly Func<int, double> _clock;
        private readonly Func<int, double> _gridLength;

        public LooperBank(int count, Func<double> clock)
            : this(count, RequireClock(clock), null)
        {
        }

        /// <param name="clock">The slot's clock. A slot must be cleared before its clock changes unit.</param>
        /// <param name="gridLength">The slot's grid length, or 0 / null for a free-length loop.</param>
        public LooperBank(int count, Func<int, double> clock, Func<int, double> gridLength)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _gridLength = gridLength;
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
                    looper.StartRecording(_clock(slot));
                    break;
                case EventLooperState.Recording:
                    // An empty or zero-length recording returns to Idle instead of playing.
                    FinishRecording(slot, looper);
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
            var recorded = 0;
            for (var slot = 0; slot < _loopers.Length; slot++)
            {
                var looper = _loopers[slot];
                if (looper.State == EventLooperState.Recording && looper.Record(payload, _clock(slot))) recorded++;
            }

            return recorded;
        }

        /// <summary>Appends the slot's events due by now. See <see cref="EventLooper{T}.Advance"/>.</summary>
        public int Advance(int slot, List<LoopPlaybackEvent<T>> output)
        {
            return this[slot].Advance(_clock(slot), output);
        }

        /// <summary>The grid length the slot uses now, or 0 for a free-length loop.</summary>
        public double GetGridLength(int slot)
        {
            RequireSlot(slot);
            var gridLength = _gridLength?.Invoke(slot) ?? 0;
            return gridLength > 0 ? gridLength : 0;
        }

        private void FinishRecording(int slot, EventLooper<T> looper)
        {
            var position = _clock(slot);
            var gridLength = GetGridLength(slot);
            if (gridLength == 0)
            {
                looper.FinishRecording(position);
                return;
            }

            var window = LoopWindow.SnapToGrid(looper.RecordStartPosition, position, gridLength);
            looper.FinishRecording(position, window.Origin, window.Duration, window.Start);
        }

        private static Func<int, double> RequireClock(Func<double> clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));

            return _ => clock();
        }

        private int RequireSlot(int slot)
        {
            if (slot < 0 || slot >= _loopers.Length) throw new ArgumentOutOfRangeException(nameof(slot));

            return slot;
        }
    }
}
