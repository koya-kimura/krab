using System;
using Krab.ShapeSynth.Core.Definitions;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>
    /// One-time resolution of random offsets into a plain Definition. Keys describe semantic parameter fields,
    /// never traversal order, so adding unrelated nodes after an occurrence keeps its values.
    /// </summary>
    public static class RandomResolver
    {
        /// <summary>Shared by every Binding delay so one occurrence moves with a single random delay.</summary>
        public const ulong DelayChannel = ulong.MaxValue;

        public static ParameterDefinition Resolve(ParameterDefinition definition, RandomContext context,
            ulong parameterKey)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!definition.HasRandom) return definition;

            // Parameter key in the upper word; component 0 is Base, i+1 is Binding i Depth.
            if (parameterKey > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(parameterKey));

            var channel = parameterKey << 32;
            var baseValue = definition.BaseValue + (definition.BaseRandom?.Resolve(context, channel) ?? 0);
            var bindings = new BindingDefinition[definition.Bindings.Count];
            for (var i = 0; i < bindings.Length; i++)
            {
                var source = definition.Bindings[i];
                var depth = source.Depth + (source.DepthRandom?.Resolve(context, channel | ((ulong)i + 1)) ?? 0);
                // One channel for every delay: all Bindings of an occurrence share its random delay.
                var delay = source.DelayBeats + (source.DelayRandom?.Resolve(context, DelayChannel) ?? 0);
                bindings[i] = new BindingDefinition(source.ModulatorIndex, depth, delayBeats: Math.Max(0, delay));
            }

            return new ParameterDefinition(definition.Reference, baseValue, bindings);
        }
    }
}
