using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
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

        private static readonly string[] RetiredColorFields =
        {
            "Red", "Green", "Blue", "Alpha", "ToRed", "ToGreen", "ToBlue", "ToAlpha", "ColorMix", "HueShift",
            "StrokeRed", "StrokeGreen", "StrokeBlue"
        };

        public static void Upgrade(JObject document)
        {
            if (document.Value<int>("SchemaVersion") == 1) UpgradeFromV1(document);
            if (document.Value<int>("SchemaVersion") == 2) UpgradeFromV2(document);
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

        // v2: RGBA Color blended to To Color by Color Mix, then hue-rotated by Hue Shift; a constant RGBA stroke.
        // v3: fill and stroke are each HSB Parameters plus a constant Alpha. The resting blend (Color Mix's Base)
        // is baked into the fill color; Color Mix's Binding and Random have no equivalent and are dropped.
        // Hue Shift (with its Binding, Random, Flip and unit) becomes the fill Hue.
        private static void UpgradeFromV2(JObject document)
        {
            if (document["RootGroup"] is JObject root) UpgradeNodeColors(root);

            document["SchemaVersion"] = 3;
        }

        private static void UpgradeNodeColors(JObject node)
        {
            var fill = RestingFill(node);
            var (fillHue, fillSaturation, fillBrightness) = ColorDefinition.ToHsb(fill);
            node["FillHue"] = HueParameter(node["HueShift"] as JObject, fillHue);
            node["FillSaturation"] = ConstantParameter(fillSaturation);
            node["FillBrightness"] = ConstantParameter(fillBrightness);
            node["FillAlpha"] = fill.A;

            var (strokeHue, strokeSaturation, strokeBrightness) = ColorDefinition.ToHsb(ReadColor(node, "Stroke"));
            node["StrokeHue"] = ConstantParameter(strokeHue);
            node["StrokeSaturation"] = ConstantParameter(strokeSaturation);
            node["StrokeBrightness"] = ConstantParameter(strokeBrightness);
            node["StrokeAlpha"] = node.Value<double?>("StrokeAlpha") ?? 1;

            foreach (var field in RetiredColorFields)
            {
                node.Remove(field);
            }

            if (!(node["Children"] is JArray children)) return;

            foreach (var child in children.OfType<JObject>())
            {
                UpgradeNodeColors(child);
            }
        }

        /// <summary>The v2 fill with Color Mix at its Base, ignoring its Binding: Color→To Color, Mix in 0–1.</summary>
        private static RgbaColor RestingFill(JObject node)
        {
            var from = ReadColor(node, "");
            var to = ReadColor(node, "To");
            var t = 0.0;
            if (node["ColorMix"] is JObject mix
                && (mix.Value<int?>("Reference") ?? 0) == (int)ReferenceSource.Absolute)
            {
                var scale = (mix.Value<double?>("Multiplier") ?? 1) * (mix.Value<double?>("AbsoluteValue") ?? 1);
                t = Math.Max(0, Math.Min(1, scale * (mix.Value<double?>("Base") ?? 0)));
            }

            return new RgbaColor(Lerp(from.R, to.R, t), Lerp(from.G, to.G, t), Lerp(from.B, to.B, t),
                Lerp(from.A, to.A, t));
        }

        // Missing v2 channels were white and opaque (1), matching the old field defaults.
        private static RgbaColor ReadColor(JObject node, string prefix)
        {
            return new RgbaColor(Channel(node, prefix + "Red"), Channel(node, prefix + "Green"),
                Channel(node, prefix + "Blue"), Channel(node, prefix + "Alpha"));
        }

        private static double Channel(JObject node, string field) => node.Value<double?>(field) ?? 1;

        private static double Lerp(double from, double to, double t) => from * (1 - t) + to * t;

        /// <summary>
        /// Keeps the old Hue Shift (Binding, Random, Flip, unit) and adds the base color's hue to its Base, so the
        /// resolved hue equals the old base hue rotated by the shift.
        /// </summary>
        private static JObject HueParameter(JObject hueShift, double baseHueDegrees)
        {
            if (hueShift == null) return ConstantParameter(baseHueDegrees);

            var parameter = (JObject)hueShift.DeepClone();
            var absolute = (parameter.Value<int?>("Reference") ?? 0) == (int)ReferenceSource.Absolute;
            var scale = (parameter.Value<double?>("Multiplier") ?? 1)
                        * (parameter.Value<double?>("AbsoluteValue") ?? 1);
            if (!absolute || scale == 0) return ConstantParameter(baseHueDegrees);

            parameter["Base"] = (parameter.Value<double?>("Base") ?? 0) + baseHueDegrees / scale;
            return parameter;
        }

        private static JObject ConstantParameter(double value) => new() { ["Base"] = value };
    }
}