using System;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    public enum ReferenceSource
    {
        Absolute,
        CanvasWidth,
        CanvasHeight,
        MinDimension,
        MaxDimension
    }

    public sealed class ReferenceDefinition
    {
        public ReferenceSource Source { get; }
        public double Multiplier { get; }
        public double AbsoluteValue { get; }

        public ReferenceDefinition(ReferenceSource source, double multiplier = 1, double absoluteValue = 1)
        {
            if (source < ReferenceSource.Absolute || source > ReferenceSource.MaxDimension)
                throw new ArgumentOutOfRangeException(nameof(source));
            VoiceTime.RequireFinite(multiplier, nameof(multiplier));
            VoiceTime.RequireFinite(absoluteValue, nameof(absoluteValue));
            Source = source;
            Multiplier = multiplier;
            AbsoluteValue = absoluteValue;
        }

        public double Evaluate(ReferenceContext context)
        {
            double value;
            switch (Source)
            {
                case ReferenceSource.Absolute:
                    value = AbsoluteValue;
                    break;
                case ReferenceSource.CanvasWidth:
                    value = context.CanvasWidth;
                    break;
                case ReferenceSource.CanvasHeight:
                    value = context.CanvasHeight;
                    break;
                case ReferenceSource.MinDimension:
                    value = Math.Min(context.CanvasWidth, context.CanvasHeight);
                    break;
                case ReferenceSource.MaxDimension:
                    value = Math.Max(context.CanvasWidth, context.CanvasHeight);
                    break;
                default:
                    throw new InvalidOperationException("Unknown reference source.");
            }

            var result = value * Multiplier;
            VoiceTime.RequireFinite(result, "referenceResult");
            return result;
        }
    }
}
