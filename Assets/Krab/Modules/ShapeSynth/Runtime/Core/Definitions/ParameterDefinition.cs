using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Voices;

namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable numeric recipe. Safe to share independently of runtime signal instances.</summary>
    public sealed class ParameterDefinition
    {
        public ReferenceDefinition Reference { get; }
        public double BaseValue { get; }

        /// <summary>Added to BaseValue once per occurrence; null for none.</summary>
        public RandomRange BaseRandom { get; }

        public IReadOnlyList<BindingDefinition> Bindings { get; }

        public bool HasRandom
        {
            get
            {
                if (BaseRandom != null) return true;

                foreach (var binding in Bindings)
                {
                    if (binding.HasRandom) return true;
                }

                return false;
            }
        }

        private ParameterDefinition(ParameterDefinition source, RandomRange baseRandom)
        {
            Reference = source.Reference;
            BaseValue = source.BaseValue;
            Bindings = source.Bindings; // The source already owns an immutable snapshot.
            BaseRandom = baseRandom ?? throw new ArgumentNullException(nameof(baseRandom));
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

        /// <summary>Returns a copy whose Base gets a per-occurrence random offset; this one is unchanged.</summary>
        public ParameterDefinition WithBaseRandom(RandomRange baseRandom) =>
            new ParameterDefinition(this, baseRandom);
    }
}
