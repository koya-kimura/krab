using System;

namespace Krab.ImageLayer.Core.Presets
{
    /// <summary>One adjustable value of a layout pattern: its name, kind, default and allowed range.</summary>
    public sealed class PatternParameter
    {
        private PatternParameter(string name, ParameterKind kind, double defaultValue, double min, double max,
            string label)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A parameter name is required.", nameof(name));

            Require.Finite(defaultValue, nameof(defaultValue));
            Require.Finite(min, nameof(min));
            Require.Finite(max, nameof(max));
            if (min > max) throw new ArgumentException("Min must not be greater than max.", nameof(min));
            if (defaultValue < min || defaultValue > max)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, "Default must be in range.");

            Name = name;
            Label = string.IsNullOrWhiteSpace(label) ? name : label;
            Kind = kind;
            Default = defaultValue;
            Min = min;
            Max = max;
        }

        /// <summary>Key in preset files; keep it when changing the label so saved values still apply.</summary>
        public string Name { get; }

        /// <summary>Text shown in the settings UI, with hints such as units or direction.</summary>
        public string Label { get; }

        public ParameterKind Kind { get; }

        public double Default { get; }

        public double Min { get; }

        public double Max { get; }

        public static PatternParameter Float(string name, double defaultValue, double min, double max,
            string label = null)
        {
            return new PatternParameter(name, ParameterKind.Float, defaultValue, min, max, label);
        }

        public static PatternParameter Int(string name, int defaultValue, int min, int max, string label = null)
        {
            return new PatternParameter(name, ParameterKind.Int, defaultValue, min, max, label);
        }

        public static PatternParameter Bool(string name, bool defaultValue, string label = null)
        {
            return new PatternParameter(name, ParameterKind.Bool, defaultValue ? 1 : 0, 0, 1, label);
        }

        /// <summary>
        /// Brings a stored or typed value into range: clamped, whole for Int, 0 or 1 for Bool. Non-finite values,
        /// such as a damaged file, fall back to the default.
        /// </summary>
        public double Normalize(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return Default;

            return Kind switch
            {
                ParameterKind.Bool => value >= 0.5 ? 1 : 0,
                ParameterKind.Int => Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), Min, Max),
                _ => Math.Clamp(value, Min, Max),
            };
        }
    }
}
