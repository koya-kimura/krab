using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>Resolves binding slots once to existing instances. Never creates or clones modulators.</summary>
    public sealed class ParameterEvaluator
    {
        private const string UnresolvedRandomMessage =
            "Resolve random offsets with a placement context before evaluating.";

        private readonly IModulator[] _signals;

        public ParameterDefinition Definition { get; }

        /// <summary>Longest Binding delay in beats; 0 when nothing is delayed.</summary>
        public double MaxDelayBeats { get; }

        public ParameterEvaluator(ParameterDefinition definition, IReadOnlyList<IModulator> modulators,
            RandomContext context, ulong parameterKey)
            : this(RandomResolver.Resolve(definition, context, parameterKey), modulators)
        {
        }

        public ParameterEvaluator(ParameterDefinition definition, IReadOnlyList<IModulator> modulators)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (modulators == null) throw new ArgumentNullException(nameof(modulators));
            if (definition.HasRandom) throw new ArgumentException(UnresolvedRandomMessage, nameof(definition));

            _signals = new IModulator[definition.Bindings.Count];
            for (var i = 0; i < _signals.Length; i++)
            {
                var binding = definition.Bindings[i];
                var slot = binding.ModulatorIndex;
                if (slot >= modulators.Count || modulators[slot] == null)
                    throw new ArgumentException("A binding refers to a missing modulator slot: " + slot, nameof(modulators));
                if (binding.DelayBeats > 0 && !(modulators[slot] is IDelayableModulator))
                    throw new ArgumentException("A delayed binding needs a delayable signal in slot " + slot,
                        nameof(modulators));

                _signals[i] = modulators[slot];
                MaxDelayBeats = Math.Max(MaxDelayBeats, binding.DelayBeats);
            }
        }

        public double Evaluate(double nowSeconds, ReferenceContext context)
        {
            VoiceTime.RequireFinite(nowSeconds, nameof(nowSeconds));
            var value = Definition.BaseValue;
            for (var i = 0; i < _signals.Length; i++)
            {
                var binding = Definition.Bindings[i];
                var signal = binding.DelayBeats > 0
                    ? ((IDelayableModulator)_signals[i]).EvaluateDelayed(nowSeconds, binding.DelayBeats)
                    : _signals[i].Evaluate(nowSeconds);
                VoiceTime.RequireFinite(signal, "signal");
                var contribution = binding.Depth * signal;
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
