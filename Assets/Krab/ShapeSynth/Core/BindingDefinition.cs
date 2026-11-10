using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>A local signal slot and signed depth. Contains no runtime signal or state.</summary>
    public sealed class BindingDefinition
    {
        public int ModulatorIndex { get; }
        public double Depth { get; }

        public BindingDefinition(int modulatorIndex, double depth)
        {
            if (modulatorIndex < 0) throw new ArgumentOutOfRangeException(nameof(modulatorIndex));
            VoiceTime.RequireFinite(depth, nameof(depth));
            ModulatorIndex = modulatorIndex;
            Depth = depth;
        }
    }
}
