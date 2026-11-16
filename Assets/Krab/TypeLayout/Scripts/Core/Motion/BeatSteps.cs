using System;

namespace Krab.TypeLayout.Core.Motion
{
    /// <summary>Converts a running beat count into discrete steps and scroll distances.</summary>
    public static class BeatSteps
    {
        /// <summary>Index of the step the beat falls in; steps last <paramref name="beatsPerStep"/> beats.</summary>
        public static long Step(double beats, double beatsPerStep)
        {
            Require.Finite(beats, nameof(beats));
            Require.Positive(beatsPerStep, nameof(beatsPerStep));

            // A tiny epsilon keeps exact boundaries (4.0 / 1.0) from flooring to the previous step.
            return (long)Math.Floor(beats / beatsPerStep + 1e-9);
        }

        /// <summary>Distance travelled at a constant speed (output pixels per beat).</summary>
        public static double Scroll(double beats, double pixelsPerBeat)
        {
            Require.Finite(beats, nameof(beats));
            Require.Finite(pixelsPerBeat, nameof(pixelsPerBeat));
            return beats * pixelsPerBeat;
        }
    }
}
