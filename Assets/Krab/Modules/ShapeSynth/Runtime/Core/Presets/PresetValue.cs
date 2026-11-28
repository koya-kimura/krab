using System;
using System.Collections.Generic;
using System.Linq;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>A named signal: the weighted sum of its ADSR / LFO components.</summary>
    [Serializable]
    public sealed class PresetValue
    {
        public string Id;
        public string Name;
        public List<PresetComponent> Components = new();

        /// <summary>A new Value with the given ID and name and copies of these Components.</summary>
        public PresetValue Duplicate(string id, string name)
        {
            PresetDocument.RequireId(id);
            return new PresetValue
            {
                Id = id,
                Name = name,
                Components = Components.Select(component => component.Clone()).ToList()
            };
        }
    }
}
