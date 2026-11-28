using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class RandomAndDelayTests
    {
        private static readonly ReferenceContext Canvas = new(100, 100);

        [Test]
        public void SeededRandomIsDeterministicInRangeAndDependsOnSeedAndPath()
        {
            var a = SeededRandom.Range(-1, 1, new RandomContext(7, 0, 1), salt: 3);

            Assert.That(SeededRandom.Range(-1, 1, new RandomContext(7, 0, 1), salt: 3), Is.EqualTo(a));
            Assert.That(a, Is.InRange(-1, 1));
            Assert.That(SeededRandom.Range(-1, 1, new RandomContext(8, 0, 1), salt: 3), Is.Not.EqualTo(a));
            Assert.That(SeededRandom.Range(-1, 1, new RandomContext(7, 0, 2), salt: 3), Is.Not.EqualTo(a));
            Assert.That(SeededRandom.Range(2, 2, new RandomContext(7)), Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomRange(1, 0));
        }

        [Test]
        public void BaseRandomIsAddedOncePerOccurrenceAndUnresolvedIsRejected()
        {
            var parameter = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 10)
                .WithBaseRandom(RandomRange.Spread(5));
            var first = new ParameterEvaluator(parameter, Array.Empty<IModulator>(), new RandomContext(1, 0), 1);
            var second = new ParameterEvaluator(parameter, Array.Empty<IModulator>(), new RandomContext(1, 1), 1);

            Assert.That(first.Evaluate(0, Canvas), Is.InRange(5, 15));
            Assert.That(first.Evaluate(9, Canvas), Is.EqualTo(first.Evaluate(0, Canvas)));
            Assert.That(second.Evaluate(0, Canvas), Is.Not.EqualTo(first.Evaluate(0, Canvas)));
            Assert.Throws<ArgumentException>(() => new ParameterEvaluator(parameter, Array.Empty<IModulator>()));
        }

        [Test]
        public void DelayReadsTheSignalEarlierAndNeedsADelayableSignal()
        {
            var parameter = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 1, delayBeats: 0.5));
            var evaluator = new ParameterEvaluator(parameter, new IModulator[] { new Clock() });

            Assert.That(evaluator.Evaluate(2, Canvas), Is.EqualTo(1.5));
            Assert.That(evaluator.MaxDelayBeats, Is.EqualTo(0.5));
            Assert.Throws<ArgumentException>(() => new ParameterEvaluator(parameter, new IModulator[] { new Plain() }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BindingDefinition(0, 1, delayBeats: -1));
        }

        [Test]
        public void DelayedAdsrStartsLaterAndVoiceLivesUntilTheLastDelayEnds()
        {
            var time = new VoiceTime(0, 60);
            var adsr = new AdsrInstance(new AdsrDefinition(1, 0, 1, 0, 0), time);

            Assert.That(adsr.EvaluateDelayed(1, 1), Is.EqualTo(0));
            Assert.That(adsr.EvaluateDelayed(1.5, 1), Is.EqualTo(adsr.Evaluate(0.5)));

            var circle = ShapeDefinition.Circle(new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute), 1, new BindingDefinition(0, 1, delayBeats: 2)));
            var target = new TriggerTarget(new GroupDefinition(null,
                new GroupRuntimeDefinition(new[] { new ModulatorSlotDefinition(new AdsrDefinition(1, 0, 1, 0, 0)) }),
                circle));
            var voice = new VoiceInstance(target, time);

            // The envelope itself ends at 1 beat; the 2-beat delay keeps the Voice alive until 3.
            Assert.That(voice.IsFinished(2.5), Is.False);
            Assert.That(voice.IsFinished(3), Is.True);
        }

        [Test]
        public void RandomDelayIsSharedByEveryBindingOfAnOccurrence()
        {
            var delay = new RandomRange(0, 4);
            var parameter = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 1, delayRandom: delay),
                new BindingDefinition(1, 1, delayRandom: delay));
            var resolved = RandomResolver.Resolve(parameter, new RandomContext(3, 2), 6);
            var other = RandomResolver.Resolve(
                new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                    new BindingDefinition(0, 1, delayRandom: delay)), new RandomContext(3, 2), 1);

            Assert.That(resolved.Bindings[0].DelayBeats, Is.InRange(0, 4));
            Assert.That(resolved.Bindings[1].DelayBeats, Is.EqualTo(resolved.Bindings[0].DelayBeats));
            Assert.That(other.Bindings[0].DelayBeats, Is.EqualTo(resolved.Bindings[0].DelayBeats));
        }

        [Test]
        public void PresetFlipReversesBoundMotionOnOddCopies()
        {
            var document = RowOfCircles(4, out var shape);
            shape.PositionY.Bind(document.Values[0].Id, 10);
            shape.PositionY.Flip = PresetFlip.Alternate;

            Assert.That(Evaluate(document, 1).Select(item => Math.Round(item.WorldTransform.M12, 9)),
                Is.EqualTo(new[] { 10.0, -10.0, 10.0, -10.0 }));
        }

        [Test]
        public void PresetDelayOrderStaggersCopiesAndRandomStaysWithinDelay()
        {
            var document = RowOfCircles(3, out var shape);
            shape.PositionY.Bind(document.Values[0].Id, 1);
            shape.Layout.Delay = 0.5;

            // Clock signal: value = now − delay.
            Assert.That(Evaluate(document, 2).Select(item => Math.Round(item.WorldTransform.M12, 9)),
                Is.EqualTo(new[] { 2.0, 1.5, 1.0 }));

            shape.Layout.DelayOrder = PresetDelayOrder.Reverse;

            Assert.That(Evaluate(document, 2).Select(item => Math.Round(item.WorldTransform.M12, 9)),
                Is.EqualTo(new[] { 1.0, 1.5, 2.0 }));

            shape.Layout.DelayOrder = PresetDelayOrder.Random;
            var values = Evaluate(document, 2).Select(item => item.WorldTransform.M12).ToArray();

            Assert.That(values, Has.All.InRange(1.5, 2.0));
            Assert.That(values.Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void PresetRandomGivesEachCopyItsOwnBaseWithinTheSpread()
        {
            var document = RowOfCircles(6, out var shape);
            shape.Rotation.Base = 90;
            shape.Rotation.Random = 45;
            var angles = Evaluate(document, 0)
                .Select(item => Math.Atan2(item.WorldTransform.M10, item.WorldTransform.M00) * 180 / Math.PI)
                .ToArray();

            Assert.That(angles, Has.All.InRange(45 - 1e-9, 135 + 1e-9));
            Assert.That(angles.Distinct().Count(), Is.EqualTo(6));
            Assert.That(Evaluate(document, 0, 1).Select(item => item.WorldTransform.M00),
                Is.Not.EqualTo(Evaluate(document, 0, 2).Select(item => item.WorldTransform.M00)));
        }

        [Test]
        public void ValidationRejectsInvalidRandomDelayAndFlip()
        {
            var negative = RowOfCircles(2, out var a);
            a.Layout.Delay = -1;
            Assert.Throws<ArgumentOutOfRangeException>(negative.Validate);

            var flip = RowOfCircles(2, out var b);
            b.Rotation.Flip = (PresetFlip)99;
            Assert.Throws<ArgumentException>(flip.Validate);

            var random = RowOfCircles(2, out var c);
            c.Rotation.Random = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(random.Validate);
        }

        private static PresetDocument RowOfCircles(int count, out PresetNode shape)
        {
            var document = PresetDocument.CreateNew();
            document.RootGroup.Children.Clear();
            shape = document.AddNode(document.RootGroup.Id, PresetNodeKind.Circle);
            shape.Layout.Kind = PresetLayoutKind.Row;
            shape.Layout.Count = count;
            shape.Layout.SpacingX = new PresetParameter { AbsoluteValue = 10, Base = 1 };
            return document;
        }

        private static IReadOnlyList<DrawItem> Evaluate(PresetDocument document, double now, int seed = 0)
        {
            var modulators = PresetCompiler.Components(document).Select(_ => (IModulator)new Clock()).ToArray();
            return new HierarchyEvaluator(PresetCompiler.CompileGroup(document), modulators, seed)
                .Evaluate(now, Canvas);
        }

        /// <summary>Signal equal to the time in seconds; one beat per second.</summary>
        private sealed class Clock : IDelayableModulator
        {
            public double Evaluate(double nowSeconds) => nowSeconds;

            public double EvaluateDelayed(double nowSeconds, double delayBeats) => nowSeconds - delayBeats;
        }

        private sealed class Plain : IModulator
        {
            public double Evaluate(double nowSeconds) => 0;
        }
    }
}
