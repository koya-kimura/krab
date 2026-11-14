using System;

namespace Krab.ShapeSynth.Core.Runtime
{
    /// <summary>Immutable trigger snapshot. Supply seconds from the same clock on every call.</summary>
    public readonly struct VoiceTime
    {
        public double StartSeconds { get; }
        public double StartBpm { get; }

        public VoiceTime(double startSeconds, double startBpm)
        {
            RequireFinite(startSeconds, nameof(startSeconds));
            RequireFinite(startBpm, nameof(startBpm));
            if (startBpm <= 0) throw new ArgumentOutOfRangeException(nameof(startBpm));
            StartSeconds = startSeconds;
            StartBpm = startBpm;
        }

        public double ElapsedBeats(double nowSeconds)
        {
            RequireFinite(nowSeconds, nameof(nowSeconds));
            if (StartBpm <= 0) throw new InvalidOperationException("VoiceTime must be constructed with a positive BPM.");
            return Math.Max(0, (nowSeconds - StartSeconds) * (StartBpm / 60.0));
        }

        internal static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
    }
}
