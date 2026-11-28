using System;

namespace Krab.Common.Looping.Core
{
    /// <summary>Where a finished recording loops: offsets from Origin, Duration long, first cycle at Start.</summary>
    public readonly struct LoopWindow
    {
        // Clock jitter around a grid line should not push playback a whole bar later.
        private const double GridTolerance = 1e-6;

        public LoopWindow(double origin, double duration, double start)
        {
            Origin = origin;
            Duration = duration;
            Start = start;
        }

        public double Origin { get; }

        public double Duration { get; }

        public double Start { get; }

        /// <summary>
        /// Snaps a recording to a grid of <paramref name="gridLength"/> (e.g. one bar in beats): the origin to
        /// the nearest grid line, the length to the nearest whole number of grid steps (at least one), and the
        /// start of playback to the first grid line at or after <paramref name="stop"/>.
        /// </summary>
        public static LoopWindow SnapToGrid(double recordStart, double stop, double gridLength)
        {
            if (!(gridLength > 0) || double.IsInfinity(gridLength))
                throw new ArgumentOutOfRangeException(nameof(gridLength), "Grid length must be positive and finite.");
            if (double.IsNaN(recordStart) || double.IsInfinity(recordStart))
                throw new ArgumentOutOfRangeException(nameof(recordStart));
            if (double.IsNaN(stop) || double.IsInfinity(stop) || stop < recordStart)
                throw new ArgumentOutOfRangeException(nameof(stop));

            var origin = Math.Floor(recordStart / gridLength + 0.5) * gridLength;
            var steps = (stop - origin) / gridLength;
            var duration = Math.Max(1, Math.Floor(steps + 0.5)) * gridLength;
            var start = origin + Math.Max(0, Math.Ceiling(steps - GridTolerance)) * gridLength;
            return new LoopWindow(origin, duration, Math.Max(start, stop));
        }
    }
}
