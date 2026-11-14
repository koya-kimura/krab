using System;
using Krab.ShapeSynth.Core.Definitions;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>One-time coefficient resolution. Keys describe semantic parameter fields, never traversal order.</summary>
    public static class VariationResolver
    {
        public static ParameterDefinition Resolve(ParameterDefinition definition, VariationContext context,
            ulong parameterKey)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var varied = definition.BaseVariation != null;
            foreach (var binding in definition.Bindings)
            {
                varied |= binding.DepthVariation != null;
            }

            if (!varied) return definition;

            // Parameter key is in the upper word; 0 denotes Base, 1+i denotes Binding Depth.
            if (parameterKey > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(parameterKey));

            var channel = parameterKey << 32;
            var baseValue = definition.BaseVariation?.Resolve(context, channel) ?? definition.BaseValue;
            var bindings = new BindingDefinition[definition.Bindings.Count];
            for (var i = 0; i < bindings.Length; i++)
            {
                var source = definition.Bindings[i];
                var depth = source.DepthVariation?.Resolve(context, channel | ((ulong)i + 1)) ?? source.Depth;
                bindings[i] = new BindingDefinition(source.ModulatorIndex, depth);
            }
            return new ParameterDefinition(definition.Reference, baseValue, bindings);
        }
    }
}
