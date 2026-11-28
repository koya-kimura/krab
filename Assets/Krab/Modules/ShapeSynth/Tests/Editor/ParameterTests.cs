using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class ParameterTests
    {
        [TestCase(ReferenceSource.Absolute, 99, 12, 1188)]
        [TestCase(ReferenceSource.CanvasWidth, 0.5, 1, 400)]
        [TestCase(ReferenceSource.CanvasHeight, 2, 1, 900)]
        [TestCase(ReferenceSource.MinDimension, 0.25, 1, 112.5)]
        [TestCase(ReferenceSource.MaxDimension, 0.1, 1, 80)]
        public void ReferenceSourcesUseAbsoluteCanvasDimensionsAndCurrentContext(
            ReferenceSource source, double multiplier, double absoluteValue, double expected)
        {
            var context = new ReferenceContext(800, 450);
            var canvasWidth = CreateEvaluator(new ReferenceDefinition(ReferenceSource.CanvasWidth), 1);
            var minDimension = CreateEvaluator(new ReferenceDefinition(ReferenceSource.MinDimension), 1);

            Assert.That(
                CreateEvaluator(new ReferenceDefinition(source, multiplier, absoluteValue), 1).Evaluate(0, context),
                Is.EqualTo(expected));
            Assert.That(canvasWidth.Evaluate(0, new ReferenceContext(200, 900)), Is.EqualTo(200));
            Assert.That(minDimension.Evaluate(0, default), Is.Zero);
        }

        [Test]
        public void ModulatorsCombineWithSignedDepthWithoutClampingOrNormalization()
        {
            var adsr = new AdsrInstance(new AdsrDefinition(1, 1, 0.5, 10, 1), new VoiceTime(0, 60), false);
            var lfo = new LfoInstance(new LfoDefinition(0.25), new VoiceTime(0, 60));
            var definition = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 2),
                1.5, new BindingDefinition(0, -2), new BindingDefinition(1, 1.5));
            var evaluator = new ParameterEvaluator(definition, new IModulator[] { adsr, lfo });

            // ADSR is 0.5 and LFO is sqrt(0.5) at this time.
            Assert.That(
                evaluator.Evaluate(0.5, default),
                Is.EqualTo(2 * (1.5 - 1 + 1.5 * Math.Sqrt(0.5))).Within(1e-12));

            var unbounded = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 1), 3,
                new BindingDefinition(0, 4)), new IModulator[] { lfo });
            Assert.That(unbounded.Evaluate(0, default), Is.EqualTo(3));
        }

        [Test]
        public void CompositeExampleAndSingleModulatorBindingsUseRealSignals()
        {
            var adsr = new AdsrInstance(new AdsrDefinition(1, 0, 1, 10, 1), new VoiceTime(0, 60), false);
            var lfo = new LfoInstance(new LfoDefinition(0, 7.0 / 12), new VoiceTime(0, 60));
            var composite = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.CanvasWidth), 0.5,
                new BindingDefinition(0, 0.5), new BindingDefinition(1, 0.2)), new IModulator[] { adsr, lfo });

            Assert.That(composite.Evaluate(0.8, new ReferenceContext(400, 200)), Is.EqualTo(320).Within(1e-12));

            var adsrOnly = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 1), 0,
                new BindingDefinition(0, 1)), new IModulator[] { adsr });
            var lfoOnly = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 1), 0,
                new BindingDefinition(0, 1)), new IModulator[] { lfo });

            Assert.That(adsrOnly.Evaluate(0.8, default), Is.EqualTo(0.8).Within(1e-12));
            Assert.That(lfoOnly.Evaluate(0, default), Is.EqualTo(-0.5).Within(1e-12));

            var negativeOutput = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 2), -1), Array.Empty<IModulator>());

            Assert.That(negativeOutput.Evaluate(0, default), Is.EqualTo(-2));
        }

        [Test]
        public void SharedDefinitionAndModulatorsRemainIndependentAcrossEvaluatorsAndRelease()
        {
            var adsr = new AdsrInstance(new AdsrDefinition(1, 0, 1, 20, 2), new VoiceTime(0, 60), false);
            var definition = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 1),
                0, new BindingDefinition(0, 1));
            var first = new ParameterEvaluator(definition, new IModulator[] { adsr });
            var second = new ParameterEvaluator(definition, new IModulator[] { adsr });

            adsr.Release(0.5);

            Assert.That(first.Evaluate(0.5, default), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(second.Evaluate(1.5, default), Is.EqualTo(0.25).Within(1e-12));

            var voiceA = new AdsrInstance(adsr.Definition, new VoiceTime(0, 60), false);
            var voiceB = new AdsrInstance(adsr.Definition, new VoiceTime(0, 60), false);
            var evalA = new ParameterEvaluator(definition, new IModulator[] { voiceA });
            var evalB = new ParameterEvaluator(definition, new IModulator[] { voiceB });

            voiceA.Release(0.5);

            Assert.That(evalA.Evaluate(1.5, default), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(evalB.Evaluate(1.5, default), Is.EqualTo(1));
        }

        [Test]
        public void DefinitionAndEvaluatorSnapshotTheirInputCollectionsAndRepeatedEvaluationIsStable()
        {
            var bindings = new[] { new BindingDefinition(0, 2) };
            var definition = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 3), 1, bindings);
            bindings[0] = new BindingDefinition(0, 100);

            Assert.That(definition.Bindings[0].Depth, Is.EqualTo(2));

            var modulator = new CountingModulator(0.5);
            var modulators = new List<IModulator> { modulator };
            var evaluator = new ParameterEvaluator(definition, modulators);
            modulators[0] = new CountingModulator(100);

            var first = evaluator.Evaluate(2, default);

            Assert.That(first, Is.EqualTo(6));
            Assert.That(evaluator.Evaluate(2, default), Is.EqualTo(first));
            Assert.That(modulator.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void SameEvaluatorRespondsToResizeAndZeroReferenceWithoutMutatingDefinition()
        {
            var definition = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.MinDimension, 0.2), 1);
            var evaluator = new ParameterEvaluator(definition, Array.Empty<IModulator>());

            Assert.That(evaluator.Evaluate(0, new ReferenceContext(1920, 1080)), Is.EqualTo(216));
            Assert.That(evaluator.Evaluate(0, new ReferenceContext(500, 800)), Is.EqualTo(100));
            Assert.That(evaluator.Evaluate(0, new ReferenceContext(0, 800)), Is.Zero);
            Assert.That(definition.BaseValue, Is.EqualTo(1));
            Assert.That(definition.Reference.Multiplier, Is.EqualTo(0.2));
        }

        [Test]
        public void DuplicateBindingsAddAndSumsAreValidatedEvenWithZeroReference()
        {
            var signal = new LfoInstance(new LfoDefinition(0, 0.25), new VoiceTime(0, 60));
            var evaluator = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 2), new BindingDefinition(0, -0.5)), new IModulator[] { signal });
            Assert.That(evaluator.Evaluate(1, default), Is.EqualTo(1.5));

            var overflowing = new ParameterEvaluator(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, 0), double.MaxValue,
                new BindingDefinition(0, double.MaxValue)), new IModulator[] { signal });

            Assert.Throws<ArgumentOutOfRangeException>(() => overflowing.Evaluate(0, default));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReferenceDefinition(ReferenceSource.Absolute, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: double.PositiveInfinity));
        }

        [Test]
        public void InvalidDefinitionsContextsTimesSignalsAndOverflowAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceDefinition((ReferenceSource)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceContext(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReferenceContext(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BindingDefinition(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BindingDefinition(0, double.NaN));
            Assert.Throws<ArgumentNullException>(() => new ParameterDefinition(null, 0));
            var absolute = new ReferenceDefinition(ReferenceSource.Absolute);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ParameterDefinition(absolute, double.PositiveInfinity));
            Assert.Throws<ArgumentNullException>(
                () => new ParameterDefinition(absolute, 0, (BindingDefinition[])null));
            Assert.Throws<ArgumentException>(
                () => new ParameterDefinition(absolute, 0, new BindingDefinition[] { null }));

            var definition = new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 1), 0,
                new BindingDefinition(0, 1));
            Assert.Throws<ArgumentNullException>(() => new ParameterEvaluator(null, Array.Empty<IModulator>()));
            Assert.Throws<ArgumentNullException>(() => new ParameterEvaluator(definition, null));
            Assert.Throws<ArgumentException>(() => new ParameterEvaluator(definition, Array.Empty<IModulator>()));
            Assert.Throws<ArgumentException>(() => new ParameterEvaluator(definition, new IModulator[] { null }));

            var evaluator = CreateEvaluator(new ReferenceDefinition(ReferenceSource.Absolute), 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Evaluate(double.NaN, default));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => evaluator.Evaluate(0, new ReferenceContext(double.PositiveInfinity, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEvaluator(
                    new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: double.MaxValue), 2)
                    .Evaluate(0, default));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEvaluator(new ReferenceDefinition(ReferenceSource.CanvasWidth, double.MaxValue), 1)
                    .Evaluate(0, new ReferenceContext(2, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ParameterEvaluator(definition, new IModulator[] { new CountingModulator(double.NaN) })
                    .Evaluate(0, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ParameterEvaluator(
                new ParameterDefinition(
                    new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: double.MaxValue), 0,
                    new BindingDefinition(0, double.MaxValue)),
                new IModulator[] { new CountingModulator(2) }).Evaluate(0, default));
        }

        private static ParameterEvaluator CreateEvaluator(ReferenceDefinition reference, double baseValue)
        {
            return new ParameterEvaluator(new ParameterDefinition(reference, baseValue), Array.Empty<IModulator>());
        }

        private sealed class CountingModulator : IModulator
        {
            private readonly double _value;

            public CountingModulator(double value)
            {
                _value = value;
            }

            public int CallCount { get; private set; }

            public double Evaluate(double nowSeconds)
            {
                CallCount++;
                return _value;
            }
        }
    }
}
