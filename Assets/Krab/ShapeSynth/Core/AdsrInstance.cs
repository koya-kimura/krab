using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>One trigger's state. Sampling is side-effect free; release events use absolute seconds.</summary>
    public sealed class AdsrInstance : IModulator, ILifetime
    {
        public AdsrDefinition Definition { get; }
        public VoiceTime Time { get; }
        private readonly bool _autoRelease;
        private double _externalReleaseBeat = double.PositiveInfinity;

        public AdsrInstance(AdsrDefinition definition, VoiceTime time, bool autoRelease = true)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            time.ElapsedBeats(time.StartSeconds); // Reject an uninitialized snapshot.
            Time = time;
            _autoRelease = autoRelease;
        }

        // First release wins. Repeated events cannot restart the release ramp.
        // Deliver events in chronological order; events before trigger are rejected.
        public void Release(double nowSeconds)
        {
            var beat = Time.ElapsedBeats(nowSeconds);
            if (nowSeconds < Time.StartSeconds) throw new ArgumentOutOfRangeException(nameof(nowSeconds));
            if (double.IsPositiveInfinity(_externalReleaseBeat)) _externalReleaseBeat = beat;
        }

        public double Evaluate(double nowSeconds)
        {
            var beat = Time.ElapsedBeats(nowSeconds);
            if (nowSeconds < Time.StartSeconds) return 0;
            var releaseBeat = ReleaseBeat;
            if (beat < releaseBeat) return BeforeRelease(beat);
            if (Definition.Release == 0 || beat - releaseBeat >= Definition.Release) return 0;
            return BeforeRelease(releaseBeat) * (1 - (beat - releaseBeat) / Definition.Release);
        }

        public bool IsFinished(double nowSeconds)
        {
            var beat = Time.ElapsedBeats(nowSeconds);
            return nowSeconds >= Time.StartSeconds && beat >= ReleaseBeat + Definition.Release;
        }

        private double ReleaseBeat => Math.Min(_externalReleaseBeat, _autoRelease
            ? Definition.Attack + Definition.Decay + Definition.SustainTime
            : double.PositiveInfinity);

        private double BeforeRelease(double beat)
        {
            if (beat < Definition.Attack) return beat / Definition.Attack;
            beat -= Definition.Attack;
            if (beat < Definition.Decay)
                return 1 + (Definition.SustainLevel - 1) * beat / Definition.Decay;
            return Definition.SustainLevel;
        }
    }
}
