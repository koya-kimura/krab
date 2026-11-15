using System.Linq;
using Newtonsoft.Json.Linq;

namespace Krab.ShapeSynth.Unity.Persistence
{
    /// <summary>Upgrades older preset JSON in place before it is deserialized.</summary>
    public static class PresetJsonMigration
    {
        private static readonly string[] ModulatorFields =
        {
            "Kind", "Attack", "Decay", "SustainLevel", "SustainTime", "Release", "CyclesPerBeat", "PhaseOffset",
            "Waveform"
        };

        public static void Upgrade(JObject document)
        {
            if (document.Value<int>("SchemaVersion") == 1) UpgradeFromV1(document);
        }

        // v1: a flat Modulator list bound by ModulatorId. v2: Values of weighted components bound by ValueId.
        private static void UpgradeFromV1(JObject document)
        {
            var values = new JArray();
            if (document["Modulators"] is JArray modulators)
            {
                foreach (var modulator in modulators.OfType<JObject>())
                {
                    var component = new JObject { ["Weight"] = 1.0 };
                    foreach (var field in ModulatorFields)
                    {
                        if (modulator[field] != null) component[field] = modulator[field];
                    }

                    values.Add(new JObject
                    {
                        ["Id"] = modulator["Id"],
                        ["Name"] = modulator["Name"],
                        ["Components"] = new JArray(component)
                    });
                }
            }

            document.Remove("Modulators");
            document["Values"] = values;
            foreach (var binding in document.Descendants().OfType<JProperty>()
                         .Where(property => property.Name == "Bindings")
                         .SelectMany(property => property.Value.OfType<JObject>())
                         .ToArray())
            {
                if (binding["ModulatorId"] == null) continue;

                binding["ValueId"] = binding["ModulatorId"];
                binding.Remove("ModulatorId");
            }

            document["SchemaVersion"] = 2;
        }
    }
}
