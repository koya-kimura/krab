using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Immutable numeric recipe. Safe to share independently of runtime signal instances.</summary>
    public sealed class ParameterDefinition
    {
        public ReferenceDefinition Reference { get; }
        public double BaseValue { get; }
        public IReadOnlyList<BindingDefinition> Bindings { get; }

        public ParameterDefinition(ReferenceDefinition reference, double baseValue, params BindingDefinition[] bindings)
        {
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            VoiceTime.RequireFinite(baseValue, nameof(baseValue));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var copy = (BindingDefinition[])bindings.Clone();
            foreach (var binding in copy)
                if (binding == null) throw new ArgumentException("Bindings cannot contain null.", nameof(bindings));
            BaseValue = baseValue;
            Bindings = Array.AsReadOnly(copy);
        }
    }
}
