using System;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// Immutable one-shot rise: 0→1 over DurationBeats from the trigger along Curve, then back to 0 at once.
    /// The rise is its lifetime.
    /// </summary>
    public sealed class RampDefinition : IModulatorDefinition
    {
        public double DurationBeats { get; }
        public EasingCurve Curve { get; }

        public RampDefinition(double durationBeats, EasingCurve curve = EasingCurve.Linear)
        {
            VoiceTime.RequireFinite(durationBeats, nameof(durationBeats));
            if (durationBeats <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationBeats), "A ramp needs a positive duration.");

            EasingFunctions.RequireDefined(curve, nameof(curve));
            DurationBeats = durationBeats;
            Curve = curve;
        }

        public IModulator Create(VoiceTime time) => new RampInstance(this, time);
    }
}
