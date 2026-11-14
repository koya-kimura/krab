using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Dedicated visual builders; all signal settings come from the independent library.</summary>
    public static class AdditionalPresets
    {
        public static TriggerTarget Pad01(ModulatorLibrary library, PresetSettings settings, int seed)
        {
            var slots = Slots(library, settings, true);
            var children = new INodeDefinition[5];
            for (var i = 0; i < children.Length; i++)
            {
                var size = 0.2 + i * 0.14;
                var diameter = Dimension(size * 0.12, size * 0.88, slots.Length > 1 ? size * 0.025 * (i + 1) : 0);
                children[i] = ShapeDefinition.Circle(diameter, opacity: Opacity(), color: Hex(0xFC4622),
                    style: new ShapeStyle(fill: false, stroke: true, strokeWidth: 4));
            }
            return Target(slots, children);
        }

        public static TriggerTarget Snare01(ModulatorLibrary library, PresetSettings settings, int seed)
        {
            var slots = Slots(library, settings, false);
            var children = new INodeDefinition[5];
            var offset = VariationDefinition.Random(0, Math.PI * 2, 80).Resolve(new VariationContext(seed));
            var triangle = new[] { new Point2(1, 0), new Point2(-0.5, Math.Sqrt(3) / 2), new Point2(-0.5, -Math.Sqrt(3) / 2) };
            for (var i = 0; i < children.Length; i++)
            {
                var angle = offset + i * Math.PI * 2 / children.Length;
                var x = Math.Cos(angle); var y = Math.Sin(angle);
                var rotation = VariationDefinition.Random(0, 360, 81).Resolve(new VariationContext(seed, i));
                var scale = Dimension(0.025, 0.015);
                children[i] = ShapeDefinition.Polygon(triangle, new TransformDefinition(
                    Dimension(x * 0.14, x * 0.22), Dimension(y * 0.14, y * 0.22),
                    Absolute(rotation, 90), scale, scale), Opacity(), Hex(0x0E551D));
            }
            return Target(slots, children);
        }

        public static TriggerTarget Lead01(ModulatorLibrary library, PresetSettings settings, int seed)
        {
            var slots = Slots(library, settings, true);
            const int count = 10;
            var vertices = new Point2[count];
            var step = Math.PI * 2 / count;
            for (var i = 0; i < count; i++)
            {
                var context = new VariationContext(seed, i);
                var angle = -Math.PI / 2 + step * i + VariationDefinition.Random(-step * 0.25, step * 0.25, 82).Resolve(context);
                var radius = (i & 1) == 0 ? VariationDefinition.Random(82, 100, 83).Resolve(context)
                    : VariationDefinition.Random(36, 48, 84).Resolve(context);
                vertices[i] = new Point2(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
            }
            // Radius is about 100 local units; full-scale stroke is about 26px at 1080p.
            var scale = Dimension(0.0003, 0.003);
            var rotation = Absolute(0, 160, slots.Length > 1 ? 20 : 0);
            var shape = ShapeDefinition.Polygon(vertices, new TransformDefinition(rotationDegrees: rotation, scaleX: scale, scaleY: scale),
                Opacity(), Hex(0x4C00D2), new ShapeStyle(fill: false, stroke: true, strokeWidth: 8));
            return Target(slots, new INodeDefinition[] { shape });
        }

        public static TriggerTarget Lead03(ModulatorLibrary library, PresetSettings settings, int seed)
        {
            var slots = Slots(library, settings, false);
            var children = new List<INodeDefinition>();
            for (var row = 0; row < 5; row++)
            for (var col = 0; col < 5; col++)
            {
                var transform = new TransformDefinition(Dimension((col - 2) * 0.14), Dimension((2 - row) * 0.14));
                children.Add(ShapeDefinition.Rectangle(Dimension(0.07, 0.03), Dimension(0.07, 0.03), transform,
                    Opacity(), Hex(0xB46600), new ShapeStyle(fill: false, stroke: true, strokeWidth: 3)));
                if (VariationDefinition.Random(0, 1, 85).Resolve(new VariationContext(seed, row, col)) < 0.35)
                    children.Add(ShapeDefinition.Circle(Dimension(0.012, 0.025), transform, Opacity(), Hex(0xC7190C)));
            }
            return Target(slots, children.ToArray());
        }

        private static ParameterDefinition Dimension(double value, double envelopeDepth = 0, double lfoDepth = 0) =>
            Parameter(ReferenceSource.MinDimension, value, envelopeDepth, lfoDepth);
        private static ParameterDefinition Absolute(double value, double envelopeDepth = 0, double lfoDepth = 0) =>
            Parameter(ReferenceSource.Absolute, value, envelopeDepth, lfoDepth);
        private static ParameterDefinition Parameter(ReferenceSource reference, double value, double envelopeDepth, double lfoDepth)
        {
            var bindings = new List<BindingDefinition>();
            if (envelopeDepth != 0) bindings.Add(new BindingDefinition(0, envelopeDepth));
            if (lfoDepth != 0) bindings.Add(new BindingDefinition(1, lfoDepth));
            return new ParameterDefinition(new ReferenceDefinition(reference), value, bindings.ToArray());
        }
        private static ParameterDefinition Opacity() => Absolute(0, 1);
        private static ModulatorSlotDefinition[] Slots(ModulatorLibrary library, PresetSettings settings, bool useLfo)
        {
            if (library == null) throw new ArgumentNullException(nameof(library));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (library.Get(settings.EnvelopeId).Adsr == null) throw new ArgumentException("Preset requires an ADSR envelope.");
            if (!useLfo || string.IsNullOrEmpty(settings.OscillatorId)) return library.ResolveSlots(settings.EnvelopeId);
            if (library.Get(settings.OscillatorId).Lfo == null) throw new ArgumentException("Preset oscillator must be an LFO.");
            return library.ResolveSlots(settings.EnvelopeId, settings.OscillatorId);
        }
        private static TriggerTarget Target(ModulatorSlotDefinition[] slots, INodeDefinition[] children) =>
            new TriggerTarget(new GroupDefinition(null, new GroupRuntimeDefinition(slots), children));
        private static RgbaColor Hex(int rgb) => new RgbaColor(((rgb >> 16) & 255) / 255.0, ((rgb >> 8) & 255) / 255.0, (rgb & 255) / 255.0);
    }
}
