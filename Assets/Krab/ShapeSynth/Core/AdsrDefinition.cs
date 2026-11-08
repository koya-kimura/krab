using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Immutable, reusable envelope settings. Durations are in beats.</summary>
    public sealed class AdsrDefinition
    {
        public double Attack { get; }
        public double Decay { get; }
        public double SustainLevel { get; }
        public double SustainTime { get; }
        public double Release { get; }

        public AdsrDefinition(double attack, double decay, double sustainLevel, double sustainTime, double release)
        {
            ValidateDuration(attack, nameof(attack));
            ValidateDuration(decay, nameof(decay));
            ValidateDuration(sustainTime, nameof(sustainTime));
            ValidateDuration(release, nameof(release));
            VoiceTime.RequireFinite(sustainLevel, nameof(sustainLevel));
            if (sustainLevel < 0 || sustainLevel > 1)
                throw new ArgumentOutOfRangeException(nameof(sustainLevel));
            VoiceTime.RequireFinite(attack + decay + sustainTime + release, "totalDuration");
            Attack = attack;
            Decay = decay;
            SustainLevel = sustainLevel;
            SustainTime = sustainTime;
            Release = release;
        }

        private static void ValidateDuration(double value, string name)
        {
            VoiceTime.RequireFinite(value, name);
            if (value < 0) throw new ArgumentOutOfRangeException(name);
        }
    }
}
