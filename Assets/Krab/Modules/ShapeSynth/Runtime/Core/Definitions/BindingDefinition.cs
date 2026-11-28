using System;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>
    /// A local signal slot, signed depth and a non-negative delay in beats (the signal is read that many beats
    /// earlier). Optional random ranges are added to Depth and Delay per occurrence. Contains no runtime state.
    /// </summary>
    public sealed class BindingDefinition
    {
        public int ModulatorIndex { get; }
        public double Depth { get; }
        public RandomRange DepthRandom { get; }
        public double DelayBeats { get; }
        public RandomRange DelayRandom { get; }

        public bool HasRandom => DepthRandom != null || DelayRandom != null;

        public BindingDefinition(int modulatorIndex, double depth, RandomRange depthRandom = null,
            double delayBeats = 0, RandomRange delayRandom = null)
        {
            if (modulatorIndex < 0) throw new ArgumentOutOfRangeException(nameof(modulatorIndex));

            VoiceTime.RequireFinite(depth, nameof(depth));
            VoiceTime.RequireFinite(delayBeats, nameof(delayBeats));
            if (delayBeats < 0) throw new ArgumentOutOfRangeException(nameof(delayBeats), "Delay cannot be negative.");
            if (delayRandom != null && delayBeats + delayRandom.Min < 0)
                throw new ArgumentOutOfRangeException(nameof(delayRandom), "Delay cannot become negative.");

            ModulatorIndex = modulatorIndex;
            Depth = depth;
            DepthRandom = depthRandom;
            DelayBeats = delayBeats;
            DelayRandom = delayRandom;
        }
    }
}
