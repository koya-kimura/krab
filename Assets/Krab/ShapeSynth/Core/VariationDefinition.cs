using System;

namespace Krab.ShapeSynth.Core
{
    /// <summary>Stateless value variation resolved from a context and optional channel.</summary>
    public sealed class VariationDefinition
    {
        private enum VariationKind
        {
            Alternating,
            Random
        }

        private readonly VariationKind _kind;
        private readonly double _first;
        private readonly double _second;
        private readonly uint _salt;

        private VariationDefinition(VariationKind kind, double first, double second, uint salt)
        {
            _kind = kind;
            _first = first;
            _second = second;
            _salt = salt;
        }

        public static VariationDefinition Alternating(double even, double odd)
        {
            VoiceTime.RequireFinite(even, nameof(even));
            VoiceTime.RequireFinite(odd, nameof(odd));
            return new VariationDefinition(VariationKind.Alternating, even, odd, 0);
        }

        public static VariationDefinition Random(double min, double max, uint salt = 0)
        {
            VoiceTime.RequireFinite(min, nameof(min));
            VoiceTime.RequireFinite(max, nameof(max));
            if (min > max) throw new ArgumentOutOfRangeException(nameof(max), "Maximum must be greater than or equal to minimum.");
            return new VariationDefinition(VariationKind.Random, min, max, salt);
        }

        /// <summary>Resolves a stable random value in [min, max); rounding can produce max.</summary>
        public double Resolve(VariationContext context, ulong channel = 0)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (_kind == VariationKind.Alternating)
                return (context.LocalIndex & 1) == 0 ? _first : _second;

            if (_first == _second) return _first;

            var hash = Mix64(unchecked((ulong)(long)context.VoiceSeed) ^ 0x9E3779B97F4A7C15UL);
            hash = Mix64(hash ^ unchecked((ulong)context.Path.Count));
            for (var i = 0; i < context.Path.Count; i++)
                hash = Mix64(hash ^ unchecked((ulong)context.Path[i]));
            hash = Mix64(hash ^ channel);
            hash = Mix64(hash ^ _salt);
            hash = Mix64(hash ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(_first)));
            hash = Mix64(hash ^ unchecked((ulong)BitConverter.DoubleToInt64Bits(_second)));

            var unit = (hash >> 11) * (1.0 / 9007199254740992.0);
            var value = _first * (1.0 - unit) + _second * unit;
            if (double.IsNaN(value)) throw new InvalidOperationException("Random interpolation produced NaN.");
            if (value < _first) return _first;
            if (value > _second) return _second;
            return value;
        }

        private static ulong Mix64(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
