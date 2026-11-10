using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Resolves binding slots once to existing instances. Never creates or clones modulators.</summary>
    public sealed class ParameterEvaluator
    {
        public ParameterDefinition Definition { get; }
        private readonly IModulator[] _signals;

        public ParameterEvaluator(ParameterDefinition definition, IReadOnlyList<IModulator> modulators)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (modulators == null) throw new ArgumentNullException(nameof(modulators));
            _signals = new IModulator[definition.Bindings.Count];
            for (var i = 0; i < _signals.Length; i++)
            {
                var slot = definition.Bindings[i].ModulatorIndex;
                if (slot >= modulators.Count || modulators[slot] == null)
                    throw new ArgumentException("A binding refers to a missing modulator slot: " + slot, nameof(modulators));
                _signals[i] = modulators[slot];
            }
        }

        public double Evaluate(double nowSeconds, ReferenceContext context)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));
            var value = Definition.BaseValue;
            for (var i = 0; i < _signals.Length; i++)
            {
                var signal = _signals[i].Evaluate(nowSeconds);
                VoiceTime.RequireFinite(signal, "signal");
                var contribution = Definition.Bindings[i].Depth * signal;
                VoiceTime.RequireFinite(contribution, "contribution");
                value += contribution;
                VoiceTime.RequireFinite(value, "parameterSum");
            }
            var result = Definition.Reference.Evaluate(context) * value;
            VoiceTime.RequireFinite(result, "parameterResult");
            return result;
        }
    }
}
