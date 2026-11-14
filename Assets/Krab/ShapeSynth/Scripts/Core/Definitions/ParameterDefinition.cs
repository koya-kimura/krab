using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable numeric recipe. Safe to share independently of runtime signal instances.</summary>
    public sealed class ParameterDefinition
    {
        public ReferenceDefinition Reference { get; }
        public double BaseValue { get; }
        public VariationDefinition BaseVariation { get; }
        public IReadOnlyList<BindingDefinition> Bindings { get; }

        private ParameterDefinition(ParameterDefinition source, VariationDefinition variation)
        {
            Reference = source.Reference;
            BaseValue = source.BaseValue;
            Bindings = source.Bindings; // The source already owns an immutable snapshot.
            BaseVariation = variation ?? throw new ArgumentNullException(nameof(variation));
        }

        public ParameterDefinition(ReferenceDefinition reference, double baseValue,
            params BindingDefinition[] bindings)
        {
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
            VoiceTime.RequireFinite(baseValue, nameof(baseValue));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));

            var copy = (BindingDefinition[])bindings.Clone();
            foreach (var binding in copy)
            {
                if (binding == null)
                    throw new ArgumentException("Bindings cannot contain null.", nameof(bindings));
            }

            BaseValue = baseValue;
            Bindings = Array.AsReadOnly(copy);
        }

        public static ParameterDefinition Constant(double value) =>
            new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), value);

        public ParameterDefinition WithBaseVariation(VariationDefinition variation) =>
            new ParameterDefinition(this, variation);
    }
}
