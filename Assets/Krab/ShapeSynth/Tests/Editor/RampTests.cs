using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class RampTests
    {
        private static readonly ReferenceContext Context = new(100, 100);

        // 120 BPM: 2 beats = 1 second, so the rise covers [10, 11).
        [TestCase(9, 0)]
        [TestCase(10, 0)]
        [TestCase(10.5, 0.25)]
        [TestCase(10.9, 0.81)]
        [TestCase(11, 0)]
        [TestCase(100000, 0)]
        public void RisesAlongItsCurveForDurationBeatsThenDropsToZero(double seconds, double expected)
        {
            Assert.That(Create().Evaluate(seconds), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void NeverHoldsOne()
        {
            var ramp = Create();
            var samples = Enumerable.Range(0, 3001).Select(i => ramp.Evaluate(9 + i * 0.001));

            Assert.That(samples.Max(), Is.LessThan(1));
        }

        [Test]
        public void FinishesExactlyWhenTheRiseEnds()
        {
            var ramp = Create();

            Assert.That(ramp.IsFinished(9), Is.False);
            Assert.That(ramp.IsFinished(10.999), Is.False);
            Assert.That(ramp.IsFinished(11), Is.True);
        }

        [Test]
        public void DelayedReadUsesTheStartBpm()
        {
            var ramp = Create();

            Assert.That(ramp.EvaluateDelayed(11, 1), Is.EqualTo(ramp.Evaluate(10.5)).Within(1e-12));
            Assert.That(ramp.EvaluateDelayed(10.25, 1), Is.EqualTo(0));
        }

        [Test]
        public void RejectsInvalidDefinitions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampDefinition(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampDefinition(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampDefinition(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampDefinition(1, (EasingCurve)999));
            Assert.Throws<ArgumentNullException>(() => new RampInstance(null, new VoiceTime(0, 60)));
        }

        [Test]
        public void ARampAloneIsAVoiceLifetime()
        {
            var root = Root(new ModulatorSlotDefinition(new RampDefinition(2)));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(0, 60));

            Assert.That(voice.Evaluate(1, Context)[0].Width, Is.EqualTo(6).Within(1e-9));
            Assert.That(voice.IsFinished(1.999), Is.False);
            Assert.That(voice.IsFinished(2), Is.True);
            Assert.That(voice.Evaluate(2, Context), Is.Empty);
        }

        [Test]
        public void AHeldEnvelopeKeepsTheVoiceAliveAfterTheRiseAndStillReleases()
        {
            var root = Root(new ModulatorSlotDefinition(new RampDefinition(2)),
                new ModulatorSlotDefinition(new AdsrDefinition(0, 0, 1, 0, 1), false));
            var voice = new VoiceInstance(new TriggerTarget(root), new VoiceTime(0, 60));

            Assert.That(voice.Evaluate(5, Context)[0].Width, Is.EqualTo(1).Within(1e-9));
            Assert.That(voice.IsFinished(100), Is.False);

            voice.Release(3);
            Assert.That(voice.IsFinished(3.5), Is.False);
            Assert.That(voice.IsFinished(4), Is.True);
        }

        [Test]
        public void PresetRampComponentCompilesAndIsEnoughToTrigger()
        {
            var document = PresetDocument.CreateNew();
            foreach (var value in document.Values)
            {
                value.Components.Clear();
            }

            var component = PresetComponent.Create(ModulatorKind.Ramp);
            component.Duration = 3;
            component.RampCurve = EasingCurve.OutBack;
            document.Values.Add(new PresetValue
            {
                Id = "rise", Name = "rise", Components = new List<PresetComponent> { component }
            });
            document.RootGroup.Children[0].Opacity.Bind("rise", 1);

            var slot = PresetCompiler.CompileGroup(document).Runtime.Modulators.Single();

            Assert.That(slot.Ramp.DurationBeats, Is.EqualTo(3));
            Assert.That(slot.Ramp.Curve, Is.EqualTo(EasingCurve.OutBack));
            Assert.DoesNotThrow(() => PresetCompiler.Compile(document));

            component.Kind = ModulatorKind.Lfo;
            Assert.Throws<ArgumentException>(() => PresetCompiler.Compile(document));
        }

        private static RampInstance Create()
        {
            return new RampInstance(new RampDefinition(2, EasingCurve.InQuad), new VoiceTime(10, 120));
        }

        // A circle of diameter 1 + 10 × slot 0.
        private static GroupDefinition Root(params ModulatorSlotDefinition[] slots)
        {
            var diameter = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 1,
                new BindingDefinition(0, 10));
            return new GroupDefinition(null, new GroupRuntimeDefinition(slots),
                new INodeDefinition[] { ShapeDefinition.Circle(diameter) });
        }
    }
}
