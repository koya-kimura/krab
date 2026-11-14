using System;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>Explicit logical canvas dimensions, independent of cameras and coordinate transforms.</summary>
    public readonly struct ReferenceContext
    {
        public double CanvasWidth { get; }
        public double CanvasHeight { get; }

        public ReferenceContext(double canvasWidth, double canvasHeight)
        {
            VoiceTime.RequireFinite(canvasWidth, nameof(canvasWidth));
            VoiceTime.RequireFinite(canvasHeight, nameof(canvasHeight));
            if (canvasWidth < 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
            if (canvasHeight < 0) throw new ArgumentOutOfRangeException(nameof(canvasHeight));
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
        }
    }
}
