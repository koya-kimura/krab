using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>One trigger's time-only lifetime, measured against its captured BPM.</summary>
    public sealed class TimerInstance : ILifetime
    {
        public TimerDefinition Definition { get; }
        public VoiceTime Time { get; }

        public TimerInstance(TimerDefinition definition, VoiceTime time)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            time.ElapsedBeats(time.StartSeconds); // Reject an uninitialized snapshot, as ADSR does.
            Time = time;
        }

        public bool IsFinished(double nowSeconds)
        {
            var elapsedBeats = Time.ElapsedBeats(nowSeconds);
            return nowSeconds >= Time.StartSeconds && elapsedBeats >= Definition.DurationBeats;
        }
    }
}
