using System;
using System.Collections.Generic;

namespace Krab.Common.Looping.Core
{
    /// <summary>
    /// Records events against a caller-owned clock and plays them back on the same time scale.
    /// Active recording and playback calls accept nondecreasing positions within each session.
    /// </summary>
    public sealed class EventLooper<T>
    {
        public const int MaxEventsPerAdvance = 256;

        private const long MaxCycle = long.MaxValue - 1;

        private readonly List<LoopPlaybackEvent<T>> _recorded = new();

        private double _lastPosition;
        private double _playbackOrigin;
        private long _nextCycle;
        private int _nextEvent;
        private bool _hasAdvanced;
        private bool _waitingForRepresentableCycle;

        public EventLooperState State { get; private set; }

        public int EventCount => _recorded.Count;

        public double Duration { get; private set; }

        public double RecordStartPosition { get; private set; }

        public double PlaybackStartPosition { get; private set; }

        public long DroppedEventCount { get; private set; }

        public void StartRecording(double position)
        {
            ValidatePosition(position);
            Clear();

            RecordStartPosition = position;
            _lastPosition = position;
            State = EventLooperState.Recording;
        }

        public bool Record(T payload, double position)
        {
            ValidatePosition(position);
            if (State != EventLooperState.Recording) return false;

            var offset = GetElapsed(position, RecordStartPosition);
            _recorded.Add(new LoopPlaybackEvent<T>(payload, offset));
            _lastPosition = position;
            return true;
        }

        public void FinishRecording(double position)
        {
            ValidatePosition(position);
            if (State != EventLooperState.Recording) return;

            var duration = GetElapsed(position, RecordStartPosition);
            if (EventCount == 0 || duration == 0)
            {
                Clear();
                return;
            }

            StartPlayback(position, duration, position);
        }

        /// <summary>
        /// Finishes into an explicit loop window, e.g. one snapped to bars. Event offsets are measured from
        /// <paramref name="origin"/> and wrapped into <paramref name="duration"/>; the first cycle starts at
        /// <paramref name="playbackStart"/>, which must not be earlier than <paramref name="position"/>.
        /// </summary>
        public void FinishRecording(double position, double origin, double duration, double playbackStart)
        {
            ValidatePosition(position);
            ValidatePosition(origin);
            ValidatePosition(playbackStart);
            if (!(duration > 0) || double.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive and finite.");
            if (playbackStart < position)
                throw new ArgumentOutOfRangeException(nameof(playbackStart), "Playback cannot start in the past.");
            if (State != EventLooperState.Recording) return;

            GetElapsed(position, RecordStartPosition);
            if (EventCount == 0)
            {
                Clear();
                return;
            }

            WrapOffsets(RecordStartPosition - origin, duration);
            StartPlayback(position, duration, playbackStart);
        }

        /// <summary>
        /// Appends the first due events in order, then drops any remaining overdue events.
        /// Returns the number of events appended to the caller's list.
        /// </summary>
        public int Advance(double position, List<LoopPlaybackEvent<T>> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            ValidatePosition(position);
            if (State != EventLooperState.Playing) return 0;

            var elapsed = GetElapsed(position, _playbackOrigin);
            if (_hasAdvanced && position == _lastPosition) return 0;

            if (_waitingForRepresentableCycle)
            {
                RebasePlayback(position, elapsed);
                _lastPosition = position;
                return 0;
            }

            var appended = 0;
            while (appended < MaxEventsPerAdvance && _nextCycle <= MaxCycle)
            {
                var scheduledPosition = GetScheduledPosition(_nextCycle, _nextEvent);
                if (scheduledPosition > position) break;

                output.Add(new LoopPlaybackEvent<T>(_recorded[_nextEvent].Payload, scheduledPosition));
                appended++;
                MoveToEvent(_nextCycle, _nextEvent + 1);
            }

            if (_nextCycle > MaxCycle ||
                (appended == MaxEventsPerAdvance && GetScheduledPosition(_nextCycle, _nextEvent) <= position))
                SkipDueEvents(position, elapsed);

            _lastPosition = position;
            _hasAdvanced = true;
            return appended;
        }

        public void Clear()
        {
            _recorded.Clear();
            _nextCycle = 0;
            _nextEvent = 0;
            _lastPosition = 0;
            _playbackOrigin = 0;
            _hasAdvanced = false;
            _waitingForRepresentableCycle = false;
            Duration = 0;
            RecordStartPosition = 0;
            PlaybackStartPosition = 0;
            DroppedEventCount = 0;
            State = EventLooperState.Idle;
        }

        private void StartPlayback(double position, double duration, double playbackStart)
        {
            Duration = duration;
            PlaybackStartPosition = playbackStart;
            _playbackOrigin = playbackStart;
            _lastPosition = position;
            State = EventLooperState.Playing;
        }

        private void WrapOffsets(double shift, double duration)
        {
            var wrapped = new List<(LoopPlaybackEvent<T> Event, int Order)>(_recorded.Count);
            for (var index = 0; index < _recorded.Count; index++)
            {
                var offset = (_recorded[index].Position + shift) % duration;
                if (offset < 0) offset += duration;
                // Rounding can land a wrapped offset exactly on the end; that is the next cycle's start.
                if (offset >= duration) offset = 0;

                wrapped.Add((new LoopPlaybackEvent<T>(_recorded[index].Payload, offset), index));
            }

            // Recording order breaks ties, so simultaneous events keep their input order after wrapping.
            wrapped.Sort((a, b) => a.Event.Position != b.Event.Position
                ? a.Event.Position.CompareTo(b.Event.Position)
                : a.Order.CompareTo(b.Order));
            _recorded.Clear();
            foreach (var entry in wrapped) _recorded.Add(entry.Event);
        }

        private void SkipDueEvents(double position, double elapsed)
        {
            if (GetScheduledPosition(MaxCycle, EventCount - 1) <= position)
            {
                DroppedEventCount = long.MaxValue;
                RebasePlayback(position, elapsed);
                return;
            }

            var cycle = FindFirstFutureCycle(_nextCycle, position);
            var nextEvent = FindFirstFutureEvent(cycle, position);
            CountDropped(cycle, nextEvent);
            MoveToEvent(cycle, nextEvent);
        }

        private void RebasePlayback(double position, double elapsed)
        {
            // Keep the loop phase while bringing future cycle indices back into range.
            _playbackOrigin = position - elapsed % Duration;
            _waitingForRepresentableCycle = GetScheduledPosition(MaxCycle, EventCount - 1) <= position;
            if (_waitingForRepresentableCycle) return;

            var cycle = FindFirstFutureCycle(0, position);
            MoveToEvent(cycle, FindFirstFutureEvent(cycle, position));
        }

        private long FindFirstFutureCycle(long low, double position)
        {
            // Find the first cycle that still has work scheduled after this position.
            // Searching by scheduled time also handles events rounded onto a cycle boundary.
            var high = MaxCycle;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (GetScheduledPosition(middle, EventCount - 1) <= position)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        private int FindFirstFutureEvent(long cycle, double position)
        {
            var low = 0;
            var high = EventCount;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (GetScheduledPosition(cycle, middle) <= position)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        private void CountDropped(long cycle, int eventIndex)
        {
            var cycles = cycle - _nextCycle;
            if (cycles == 0)
            {
                AddDropped(eventIndex - _nextEvent);
                return;
            }

            AddDropped(EventCount - _nextEvent);
            AddDropped(eventIndex);

            var wholeCycles = cycles - 1;
            var capacity = long.MaxValue - DroppedEventCount;
            DroppedEventCount += wholeCycles > capacity / EventCount ? capacity : wholeCycles * EventCount;
        }

        private void AddDropped(long count)
        {
            DroppedEventCount += Math.Min(count, long.MaxValue - DroppedEventCount);
        }

        private void MoveToEvent(long cycle, int eventIndex)
        {
            _nextCycle = cycle;
            _nextEvent = eventIndex;
            if (eventIndex == EventCount)
            {
                _nextCycle++;
                _nextEvent = 0;
            }
        }

        private double GetScheduledPosition(long cycle, int eventIndex)
        {
            var offset = _recorded[eventIndex].Position;
            var cycleEnd = (cycle + 1) * Duration;
            if (offset == Duration) return _playbackOrigin + cycleEnd;

            return _playbackOrigin + Math.Min(cycle * Duration + offset, cycleEnd);
        }

        private double GetElapsed(double position, double origin)
        {
            var elapsed = position - origin;
            if (position < _lastPosition || double.IsInfinity(elapsed))
                throw new ArgumentOutOfRangeException(nameof(position), "Position must advance within a finite range.");

            return elapsed;
        }

        private static void ValidatePosition(double position)
        {
            if (double.IsNaN(position) || double.IsInfinity(position))
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be finite.");
        }
    }
}
