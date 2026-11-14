using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Performance;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public class Fx01Tests
    {
        private static readonly ReferenceContext Context = new ReferenceContext(1920, 1080);

        private static TriggerTarget CreateTarget()
        {
            var settings = PerformanceSettings.CreateDefault();
            return Fx01Preset.Build(settings.BuildLibrary(), settings.Fx01);
        }

        [Test]
        public void DefaultFx01Builds150CirclesInTwoExactAlternatingColors()
        {
            var settings = PerformanceSettings.CreateDefault();
            var target = Fx01Preset.Build(settings.BuildLibrary(), settings.Fx01);
            Assert.That(target.Definition.Children, Has.Count.EqualTo(150));
            var voice = new VoiceInstance(target, new VoiceTime(0, 120), settings.Fx01.Seed);
            var items = voice.Evaluate(0, Context);
            Assert.That(items, Has.Count.EqualTo(150));
            Assert.That(items.All(item => item.Kind == ShapeKind.Circle), Is.True);
            var red = new RgbaColor(199.0 / 255, 25.0 / 255, 12.0 / 255);
            var cyan = new RgbaColor(57.0 / 255, 225.0 / 255, 191.0 / 255);
            Assert.That(items.Count(item => item.Color.Equals(red)), Is.EqualTo(75));
            Assert.That(items.Count(item => item.Color.Equals(cyan)), Is.EqualTo(75));
            Assert.That(items[0].Color, Is.EqualTo(red));
            Assert.That(items[1].Color, Is.EqualTo(cyan));
        }

        [Test]
        public void SeedReproducesPositionsAndDifferentSeedChangesPositions()
        {
            var target = CreateTarget();
            var sameA = new VoiceInstance(target, new VoiceTime(0, 120), 1234).Evaluate(0, Context);
            var sameB = new VoiceInstance(target, new VoiceTime(0, 120), 1234).Evaluate(0, Context);
            var different = new VoiceInstance(target, new VoiceTime(0, 120), 1235).Evaluate(0, Context);
            for (var i = 0; i < sameA.Count; i++)
            {
                Assert.That(sameA[i].WorldTransform.M02, Is.EqualTo(sameB[i].WorldTransform.M02));
                Assert.That(sameA[i].WorldTransform.M12, Is.EqualTo(sameB[i].WorldTransform.M12));
            }
            Assert.That(sameA[0].WorldTransform.M02, Is.Not.EqualTo(different[0].WorldTransform.M02));
        }

        [Test]
        public void MultipleSpawnsHaveIndependentStateAndExpireAtInclusiveEnvelopeEnd()
        {
            var target = CreateTarget();
            var collection = new VoiceCollection();
            var first = collection.Spawn(target, 0, 120, 1);
            var second = collection.Spawn(target, 1, 120, 2);
            Assert.That(collection.Voices, Has.Count.EqualTo(2));
            Assert.That(first.Modulators[0], Is.Not.SameAs(second.Modulators[0]));
            Assert.That(first.Modulators[1], Is.Not.SameAs(second.Modulators[1]));
            Assert.That(first.IsFinished(2.299), Is.False);
            Assert.That(first.IsFinished(2.3), Is.True);
            collection.Evaluate(2.3, Context);
            Assert.That(collection.Voices, Has.Count.EqualTo(1));
            Assert.That(collection.Voices[0], Is.SameAs(second));
        }

        [Test]
        public void PresetBuildDoesNotMutateReusableModulatorDefinitions()
        {
            var settings = PerformanceSettings.CreateDefault();
            var library = settings.BuildLibrary();
            var envelope = library.Get(settings.Fx01.EnvelopeId).Adsr;
            var oscillator = library.Get(settings.Fx01.OscillatorId).Lfo;
            Fx01Preset.Build(library, settings.Fx01);
            Fx01Preset.Build(library, settings.Fx01);
            Assert.That(library.Get(settings.Fx01.EnvelopeId).Adsr, Is.SameAs(envelope));
            Assert.That(library.Get(settings.Fx01.OscillatorId).Lfo, Is.SameAs(oscillator));
            Assert.That(envelope.Attack, Is.EqualTo(0.1));
            Assert.That(oscillator.CyclesPerBeat, Is.EqualTo(0.5));
        }
    }
}
