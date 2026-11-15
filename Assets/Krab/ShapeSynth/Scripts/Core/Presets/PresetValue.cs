using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>A named signal: the weighted sum of its ADSR / LFO components.</summary>
    [Serializable]
    public sealed class PresetValue
    {
        public string Id;
        public string Name;
        public List<PresetComponent> Components = new();
    }
}
