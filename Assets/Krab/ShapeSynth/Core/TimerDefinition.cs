using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Immutable timer settings. Duration is expressed in beats.</summary>
    public sealed class TimerDefinition
    {
        public double DurationBeats { get; }

        public TimerDefinition(double durationBeats)
        {
            VoiceTime.RequireFinite(durationBeats, nameof(durationBeats));
            if (durationBeats < 0) throw new ArgumentOutOfRangeException(nameof(durationBeats));
            DurationBeats = durationBeats;
        }
    }
}
