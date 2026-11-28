using System;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// Immutable, reusable envelope settings. Durations are in beats. Each moving segment follows its own curve from
    /// its start to its end level; Linear for all three is the classic straight-line ADSR.
    /// </summary>
    public sealed class AdsrDefinition : IModulatorDefinition
    {
        public double Attack { get; }
        public double Decay { get; }
        public double SustainLevel { get; }
        public double SustainTime { get; }
        public double Release { get; }
        public EasingCurve AttackCurve { get; }
        public EasingCurve DecayCurve { get; }
        public EasingCurve ReleaseCurve { get; }

        public AdsrDefinition(double attack, double decay, double sustainLevel, double sustainTime, double release,
            EasingCurve attackCurve = EasingCurve.Linear, EasingCurve decayCurve = EasingCurve.Linear,
            EasingCurve releaseCurve = EasingCurve.Linear)
        {
            ValidateDuration(attack, nameof(attack));
            ValidateDuration(decay, nameof(decay));
            ValidateDuration(sustainTime, nameof(sustainTime));
            ValidateDuration(release, nameof(release));
            VoiceTime.RequireFinite(sustainLevel, nameof(sustainLevel));
            if (sustainLevel < 0 || sustainLevel > 1)
                throw new ArgumentOutOfRangeException(nameof(sustainLevel));

            VoiceTime.RequireFinite(attack + decay + sustainTime + release, "totalDuration");
            EasingFunctions.RequireDefined(attackCurve, nameof(attackCurve));
            EasingFunctions.RequireDefined(decayCurve, nameof(decayCurve));
            EasingFunctions.RequireDefined(releaseCurve, nameof(releaseCurve));
            Attack = attack;
            Decay = decay;
            SustainLevel = sustainLevel;
            SustainTime = sustainTime;
            Release = release;
            AttackCurve = attackCurve;
            DecayCurve = decayCurve;
            ReleaseCurve = releaseCurve;
        }

        /// <summary>Creates an auto-releasing envelope; a slot can request a held one instead.</summary>
        public IModulator Create(VoiceTime time) => new AdsrInstance(this, time);

        private static void ValidateDuration(double value, string name)
        {
            VoiceTime.RequireFinite(value, name);
            if (value < 0) throw new ArgumentOutOfRangeException(name);
        }
    }
}
