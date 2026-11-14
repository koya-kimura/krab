using System;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>A local signal slot and signed depth. Contains no runtime signal or state.</summary>
    public sealed class BindingDefinition
    {
        public int ModulatorIndex { get; }
        public double Depth { get; }
        public VariationDefinition DepthVariation { get; }

        public BindingDefinition(int modulatorIndex, double depth, VariationDefinition depthVariation = null)
        {
            if (modulatorIndex < 0) throw new ArgumentOutOfRangeException(nameof(modulatorIndex));
            VoiceTime.RequireFinite(depth, nameof(depth));
            ModulatorIndex = modulatorIndex;
            Depth = depth;
            DepthVariation = depthVariation;
        }
    }
}
