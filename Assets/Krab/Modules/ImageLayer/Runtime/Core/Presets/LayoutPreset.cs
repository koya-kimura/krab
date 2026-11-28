using System;

namespace Krab.ImageLayer.Core.Presets
{
    /// <summary>A named arrangement: which pattern draws and with which parameter values. Holds no images.</summary>
    public sealed class LayoutPreset
    {
        private string _name;

        public LayoutPreset(string id, string name, string pattern, ParameterValues values)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A preset id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("A pattern is required.", nameof(pattern));

            Id = id;
            Name = name;
            Pattern = pattern;
            Values = Require.NotNull(values, nameof(values));
        }

        /// <summary>Stable key for files and MIDI bindings; never changes after creation.</summary>
        public string Id { get; }

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? Id : value.Trim();
        }

        public string Pattern { get; }

        public ParameterValues Values { get; }

        public LayoutPreset Duplicate(string id, string name) => new(id, name, Pattern, Values.Copy());
    }
}
