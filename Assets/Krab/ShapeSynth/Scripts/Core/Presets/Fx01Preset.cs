using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Runtime;

namespace Krab.ShapeSynth.Core.Presets
{
    /// <summary>Builds the reusable two-color circle preset.</summary>
    public static class Fx01Preset
    {
        private const uint PositionXBaseSalt = 0xF001;
        private const uint PositionYBaseSalt = 0xF002;
        private const uint DiameterSalt = 0xF003;
        private const uint PositionXEnvelopeSalt = 0xF011;
        private const uint PositionXOscillatorSalt = 0xF012;
        private const uint PositionYEnvelopeSalt = 0xF021;
        private const uint PositionYOscillatorSalt = 0xF022;

        public static TriggerTarget Build(ModulatorLibrary library, Fx01Settings settings)
        {
            if (library == null) throw new ArgumentNullException(nameof(library));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var circleCount = settings.CircleCount;
            var envelopeId = settings.EnvelopeId;
            var oscillatorId = settings.OscillatorId;

            if (circleCount < 1 || circleCount > 2000)
                throw new ArgumentOutOfRangeException(nameof(settings.CircleCount), circleCount,
                    "Circle count must be between 1 and 2000.");
            if (string.IsNullOrWhiteSpace(envelopeId))
                throw new ArgumentException("An envelope modulator ID is required.", nameof(settings));
            if (string.IsNullOrWhiteSpace(oscillatorId))
                throw new ArgumentException("An oscillator modulator ID is required.", nameof(settings));

            var envelopeEntry = library.Get(envelopeId);
            var oscillatorEntry = library.Get(oscillatorId);
            if (envelopeEntry == null)
                throw new ArgumentException($"Envelope modulator '{envelopeId}' was not found.", nameof(settings));
            if (oscillatorEntry == null)
                throw new ArgumentException($"Oscillator modulator '{oscillatorId}' was not found.", nameof(settings));
            if (envelopeEntry.Slot?.Adsr == null)
                throw new ArgumentException($"Modulator '{envelopeId}' must be an ADSR.", nameof(settings));
            if (oscillatorEntry.Slot?.Lfo == null)
                throw new ArgumentException($"Modulator '{oscillatorId}' must be an LFO.", nameof(settings));

            var slots = library.ResolveSlots(envelopeId, oscillatorId);
            var children = new INodeDefinition[circleCount];
            var red = CreateCircle(0, new RgbaColor(199.0 / 255, 25.0 / 255, 12.0 / 255));
            var cyan = CreateCircle(1, new RgbaColor(57.0 / 255, 225.0 / 255, 191.0 / 255));
            for (var i = 0; i < children.Length; i++)
                children[i] = (i & 1) == 0 ? red : cyan;

            var root = new GroupDefinition(null, new GroupRuntimeDefinition(slots), children);
            return new TriggerTarget(root);
        }

        private static ShapeDefinition CreateCircle(int channel, RgbaColor color)
        {
            var positionX = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.CanvasWidth), 0,
                new BindingDefinition(0, 1, VariationDefinition.Random(-0.04, 0.04, PositionXEnvelopeSalt)),
                new BindingDefinition(1, 1, VariationDefinition.Random(-0.015, 0.015, PositionXOscillatorSalt)))
                .WithBaseVariation(VariationDefinition.Random(-0.48, 0.48, PositionXBaseSalt));
            var positionY = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.CanvasHeight), 0,
                new BindingDefinition(0, 1, VariationDefinition.Random(-0.04, 0.04, PositionYEnvelopeSalt)),
                new BindingDefinition(1, 1, VariationDefinition.Random(-0.015, 0.015, PositionYOscillatorSalt)))
                .WithBaseVariation(VariationDefinition.Random(-0.48, 0.48, PositionYBaseSalt));
            var diameter = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.MinDimension), 0)
                .WithBaseVariation(VariationDefinition.Random(0.005, 0.02, DiameterSalt + (uint)channel));
            var opacity = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 1));

            return ShapeDefinition.Circle(diameter,
                new TransformDefinition(positionX, positionY), opacity, color);
        }
    }
}
