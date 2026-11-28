using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Presets
{
    /// <summary>
    /// Parameter values of one layout preset, by parameter name. A missing value reads as the parameter's default;
    /// values of parameters a pattern no longer has are kept so an older file loses nothing.
    /// </summary>
    public sealed class ParameterValues
    {
        private readonly Dictionary<string, double> _values = new(StringComparer.Ordinal);

        public ParameterValues()
        {
        }

        public ParameterValues(IEnumerable<KeyValuePair<string, double>> values)
        {
            foreach (var pair in Require.NotNull(values, nameof(values)))
                _values[pair.Key] = pair.Value;
        }

        public IReadOnlyDictionary<string, double> Stored => _values;

        public static ParameterValues Defaults(IEnumerable<PatternParameter> parameters)
        {
            var values = new ParameterValues();
            foreach (var parameter in Require.NotNull(parameters, nameof(parameters)))
                values._values[parameter.Name] = parameter.Default;

            return values;
        }

        public double Get(PatternParameter parameter)
        {
            Require.NotNull(parameter, nameof(parameter));
            return _values.TryGetValue(parameter.Name, out var value) ? parameter.Normalize(value) : parameter.Default;
        }

        public float GetFloat(PatternParameter parameter) => (float)Get(parameter);

        public int GetInt(PatternParameter parameter) => (int)Get(parameter);

        public bool GetBool(PatternParameter parameter) => Get(parameter) >= 0.5;

        public void Set(PatternParameter parameter, double value)
        {
            Require.NotNull(parameter, nameof(parameter));
            _values[parameter.Name] = parameter.Normalize(value);
        }

        public ParameterValues Copy() => new(_values);
    }
}
