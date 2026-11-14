using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public class VariationTests
    {
        [Test]
        public void ContextUsesRootZeroLastPathIndexAndImmutableChildren()
        {
            var source = new[] { 3, 4 };
            var context = new VariationContext(9, source);
            source[1] = 99;
            Assert.That(context.LocalIndex, Is.EqualTo(4));
            Assert.That(context.Child(7).Path, Is.EqualTo(new[] { 3, 4, 7 }));
            Assert.That(context.Path, Is.EqualTo(new[] { 3, 4 }));
            Assert.That(new VariationContext().LocalIndex, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => context.Child(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VariationContext(0, 1, -1));
            Assert.Throws<ArgumentNullException>(() => new VariationContext(0, (int[])null));
        }

        [Test]
        public void AlternatingUsesLocalIndexAndRestartsAtEachGroup()
        {
            var variation = VariationDefinition.Alternating(10, 20);
            Assert.That(variation.Resolve(new VariationContext(0)), Is.EqualTo(10));
            Assert.That(variation.Resolve(new VariationContext(0, 0)), Is.EqualTo(10));
            Assert.That(variation.Resolve(new VariationContext(0, 1)), Is.EqualTo(20));
            Assert.That(variation.Resolve(new VariationContext(0, 5, 0)), Is.EqualTo(10));
            Assert.That(variation.Resolve(new VariationContext(0, 5, 1)), Is.EqualTo(20));
        }

        [Test]
        public void ReusedShapeDefinitionVariesByOccurrenceAndAppendingSiblingKeepsExistingPaths()
        {
            var shared = ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Alternating(2, 8)));
            var first = new HierarchyEvaluator(new GroupDefinition(null, shared, shared), Array.Empty<IModulator>()).Evaluate(0, default);
            var extended = new HierarchyEvaluator(new GroupDefinition(null, shared, shared, ShapeDefinition.Circle(ParameterDefinition.Constant(1))), Array.Empty<IModulator>()).Evaluate(0, default);
            Assert.That(first[0].Width, Is.EqualTo(2));
            Assert.That(first[1].Width, Is.EqualTo(8));
            Assert.That(extended[0].Width, Is.EqualTo(first[0].Width));
            Assert.That(extended[1].Width, Is.EqualTo(first[1].Width));
        }

        [Test]
        public void NestedShapesUseSiblingIndexRatherThanFlattenedTraversalIndex()
        {
            var varied = ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Alternating(3, 7)));
            var root = new GroupDefinition(null, new GroupDefinition(null, varied, varied), varied);
            var items = new HierarchyEvaluator(root, Array.Empty<IModulator>()).Evaluate(0, default);
            Assert.That(items[0].Width, Is.EqualTo(3));
            Assert.That(items[1].Width, Is.EqualTo(7));
            Assert.That(items[2].Width, Is.EqualTo(7));
        }

        [Test]
        public void RandomVariationIsStableAndDependsOnVoiceSeedPathParameterChannelAndSalt()
        {
            var random = VariationDefinition.Random(-10, 10, 3);
            var context = new VariationContext(123, 2, 5);
            var same = random.Resolve(context, 6);
            Assert.That(random.Resolve(context, 6), Is.EqualTo(same));
            Assert.That(same, Is.EqualTo(4.825484788991554).Within(1e-12));
            Assert.That(random.Resolve(new VariationContext(124, 2, 5), 6), Is.Not.EqualTo(same));
            Assert.That(random.Resolve(new VariationContext(123, 2, 6), 6), Is.Not.EqualTo(same));
            Assert.That(random.Resolve(context, 7), Is.Not.EqualTo(same));
            Assert.That(VariationDefinition.Random(-10, 10, 4).Resolve(context, 6), Is.Not.EqualTo(same));
            Assert.That(same, Is.InRange(-10, 10));
        }

        [Test]
        public void RandomVariationDoesNotDependOnResolutionOrder()
        {
            var random = VariationDefinition.Random(0, 1);
            var context = new VariationContext(83, 4, 2);
            var first = random.Resolve(context, 1);
            random.Resolve(context, 99);
            random.Resolve(new VariationContext(83, 0), 2);
            Assert.That(random.Resolve(context, 1), Is.EqualTo(first));
        }

        [Test]
        public void RandomSiblingAppendDoesNotChangeExistingPlacementValues()
        {
            var randomShape = ShapeDefinition.Circle(ParameterDefinition.Constant(0)
                .WithBaseVariation(VariationDefinition.Random(1, 2, 8)));
            var before = new HierarchyEvaluator(new GroupDefinition(null, randomShape, randomShape),
                Array.Empty<IModulator>(), 17).Evaluate(0, default);
            var after = new HierarchyEvaluator(new GroupDefinition(null, randomShape, randomShape,
                ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Random(1, 2, 8)))),
                Array.Empty<IModulator>(), 17).Evaluate(0, default);
            Assert.That(after[0].Width, Is.EqualTo(before[0].Width));
            Assert.That(after[1].Width, Is.EqualTo(before[1].Width));
        }

        [Test]
        public void RandomVariationHandlesConstantAndExtremeFiniteRanges()
        {
            Assert.That(VariationDefinition.Random(5, 5).Resolve(new VariationContext()), Is.EqualTo(5));
            var extreme = VariationDefinition.Random(-double.MaxValue, double.MaxValue);
            for (var i = 0; i < 20; i++)
            {
                var value = extreme.Resolve(new VariationContext(i, i, 8));
                Assert.That(double.IsNaN(value) || double.IsInfinity(value), Is.False);
                Assert.That(value, Is.InRange(-double.MaxValue, double.MaxValue));
            }
        }

        [Test]
        public void VariationDefinitionsRejectInvalidNumbersAndContexts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VariationDefinition.Alternating(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => VariationDefinition.Alternating(0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => VariationDefinition.Random(2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => VariationDefinition.Random(0, double.NegativeInfinity));
            Assert.Throws<ArgumentNullException>(() => VariationDefinition.Random(0, 1).Resolve(null));
        }

        [Test]
        public void ResolverVariesBaseAndBindingDepthWithoutChangingSourceDefinition()
        {
            var depthVariation = VariationDefinition.Alternating(2, 6);
            var definition = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute, absoluteValue: 3), 5,
                new BindingDefinition(0, 1, depthVariation));
            var resolved = VariationResolver.Resolve(definition, new VariationContext(0, 1), 2);
            Assert.That(resolved.BaseValue, Is.EqualTo(5));
            Assert.That(resolved.Bindings[0].Depth, Is.EqualTo(6));
            Assert.That(definition.Bindings[0].Depth, Is.EqualTo(1));
            Assert.That(definition.Bindings[0].DepthVariation, Is.SameAs(depthVariation));
        }

        [Test]
        public void BaseVariationCanBeAppliedImmutablyAndAlternatesAtPlacement()
        {
            var original = ParameterDefinition.Constant(4);
            var varied = original.WithBaseVariation(VariationDefinition.Alternating(9, 12));
            Assert.That(original.BaseVariation, Is.Null);
            Assert.That(original.BaseValue, Is.EqualTo(4));
            Assert.That(VariationResolver.Resolve(varied, new VariationContext(0, 1), 1).BaseValue, Is.EqualTo(12));
            Assert.Throws<ArgumentNullException>(() => original.WithBaseVariation(null));
        }

        [Test]
        public void ParameterEvaluatorResolvesOnceAndRetainsDynamicReferenceContext()
        {
            var definition = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth), 0)
                .WithBaseVariation(VariationDefinition.Alternating(2, 4));
            var evaluator = new ParameterEvaluator(definition, Array.Empty<IModulator>(), new VariationContext(0, 1), 6);
            Assert.That(evaluator.Evaluate(0, new ReferenceContext(100, 20)), Is.EqualTo(400));
            Assert.That(evaluator.Evaluate(0, new ReferenceContext(25, 30)), Is.EqualTo(100));
            Assert.That(evaluator.Definition.BaseValue, Is.EqualTo(4));
            Assert.That(definition.BaseValue, Is.Zero);
            Assert.Throws<ArgumentException>(() => new ParameterEvaluator(definition, Array.Empty<IModulator>()));
        }

        [Test]
        public void ParameterVariationRejectsOversizedParameterKey()
        {
            var definition = ParameterDefinition.Constant(1).WithBaseVariation(VariationDefinition.Alternating(2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => VariationResolver.Resolve(definition, new VariationContext(), (ulong)uint.MaxValue + 1));
        }

        [Test]
        public void HierarchyUsesStableSemanticChannelsAndCircleSharesDiameterChannel()
        {
            var random = VariationDefinition.Random(1, 10, 55);
            var circle = ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(random));
            var rect = ShapeDefinition.Rectangle(ParameterDefinition.Constant(0).WithBaseVariation(random),
                ParameterDefinition.Constant(0).WithBaseVariation(random));
            var items = new HierarchyEvaluator(new GroupDefinition(null, circle, rect), Array.Empty<IModulator>(), 91).Evaluate(0, default);
            Assert.That(items[0].Width, Is.EqualTo(items[0].Height));
            Assert.That(items[1].Width, Is.Not.EqualTo(items[1].Height));
            var again = new HierarchyEvaluator(new GroupDefinition(null, circle, rect), Array.Empty<IModulator>(), 91).Evaluate(0, default);
            Assert.That(items[0].Width, Is.EqualTo(again[0].Width));
            Assert.That(items[1].Height, Is.EqualTo(again[1].Height));
        }

        [Test]
        public void HierarchyVariationDoesNotChangeModulatorBindingOrScope()
        {
            var lfo = new LfoInstance(new LfoDefinition(0.25), new VoiceTime(0, 60));
            var variedDepth = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 0, VariationDefinition.Alternating(2, 4)));
            var group = new GroupDefinition(null,
                ShapeDefinition.Circle(ParameterDefinition.Constant(1), new TransformDefinition(positionX: variedDepth)),
                ShapeDefinition.Circle(ParameterDefinition.Constant(1), new TransformDefinition(positionX: variedDepth)));
            var items = new HierarchyEvaluator(group, new IModulator[] { lfo }).Evaluate(1, default);
            Assert.That(items[0].WorldTransform.TransformPoint(new Point2(0, 0)).X, Is.EqualTo(2).Within(1e-12));
            Assert.That(items[1].WorldTransform.TransformPoint(new Point2(0, 0)).X, Is.EqualTo(4).Within(1e-12));
        }

        [Test]
        public void RootAndGroupTransformVariationsUseTheirOwnPlacementIndices()
        {
            var alternating = VariationDefinition.Alternating(5, 9);
            var root = new GroupDefinition(new TransformDefinition(positionX:
                    ParameterDefinition.Constant(0).WithBaseVariation(alternating)),
                ShapeDefinition.Circle(ParameterDefinition.Constant(1), new TransformDefinition(positionX:
                    ParameterDefinition.Constant(0).WithBaseVariation(alternating))),
                new GroupDefinition(new TransformDefinition(positionX:
                    ParameterDefinition.Constant(0).WithBaseVariation(alternating)),
                    ShapeDefinition.Circle(ParameterDefinition.Constant(1))));
            var items = new HierarchyEvaluator(root, Array.Empty<IModulator>()).Evaluate(0, default);
            // Root local index is zero; its direct shape is child zero and the nested group is child one.
            Assert.That(items[0].WorldTransform.TransformPoint(new Point2(0, 0)).X, Is.EqualTo(10));
            Assert.That(items[1].WorldTransform.TransformPoint(new Point2(0, 0)).X, Is.EqualTo(14));
        }

        [Test]
        public void VoiceCollectionSeedFlowsIntoVariationsAcrossSpawnAndRestart()
        {
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Random(0, 1)));
            var runtime = new GroupRuntimeDefinition(timers: new[] { new TimerDefinition(100) });
            var target = new TriggerTarget(new GroupDefinition(null, runtime,
                new GroupDefinition(null, shape), shape));
            var voices = new VoiceCollection();
            var a = voices.Spawn(target, 0, 60, 42);
            var b = voices.Spawn(target, 0, 60, 42);
            var aValue = a.Evaluate(0, default)[0].Width;
            var bValue = b.Evaluate(0, default)[0].Width;
            Assert.That(aValue, Is.EqualTo(bValue));
            var c = voices.Restart(target, 0, 60, 43);
            var cValue = c.Evaluate(0, default)[0].Width;
            Assert.That(a.VoiceSeed, Is.EqualTo(42));
            Assert.That(b.VoiceSeed, Is.EqualTo(42));
            Assert.That(c.VoiceSeed, Is.EqualTo(43));
            Assert.That(cValue, Is.Not.EqualTo(aValue));
            Assert.That(a.IsFinished(0), Is.True);
            Assert.That(c.IsFinished(0), Is.False);
            Assert.That(voices.Voices, Has.Count.EqualTo(1));
        }

        [Test]
        public void VoiceVariationIsFrozenAcrossRepeatedTimeEvaluationAndSharedBindings()
        {
            var adsr = new AdsrDefinition(1, 0, 1, 10, 1);
            var lfo = new LfoDefinition(0.25);
            var runtime = new GroupRuntimeDefinition(new[]
            {
                new ModulatorSlotDefinition(adsr, false),
                new ModulatorSlotDefinition(lfo)
            }, new[] { new TimerDefinition(100) });
            var parameter = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.Absolute), 0,
                new BindingDefinition(0, 0.25, VariationDefinition.Random(0.5, 1)),
                new BindingDefinition(1, 0.5, VariationDefinition.Random(1, 2)));
            var target = new TriggerTarget(new GroupDefinition(null, runtime,
                ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Random(2, 3)),
                    new TransformDefinition(positionX: parameter))));
            var voice = new VoiceInstance(target, new VoiceTime(0, 60), 4);
            Assert.That(voice.Modulators, Has.Count.EqualTo(2));
            var t1 = voice.Evaluate(0.5, default)[0];
            var t2 = voice.Evaluate(0.5, default)[0];
            Assert.That(t2.Width, Is.EqualTo(t1.Width));
            Assert.That(t2.WorldTransform.TransformPoint(new Point2(0, 0)).X,
                Is.EqualTo(t1.WorldTransform.TransformPoint(new Point2(0, 0)).X));
            Assert.That(voice.Evaluate(0.75, default)[0].Width, Is.EqualTo(t1.Width));
        }

        [Test]
        public void VariationDoesNotChangeVoiceLifetimeEvaluationOrReleaseBehavior()
        {
            var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(0).WithBaseVariation(VariationDefinition.Random(1, 2)));
            var runtime = new GroupRuntimeDefinition(new[] { new ModulatorSlotDefinition(new AdsrDefinition(1, 0, 1, 1, 1), false) });
            var target = new TriggerTarget(new GroupDefinition(null, runtime, shape));
            var voice = new VoiceInstance(target, new VoiceTime(0, 60), 12);
            Assert.That(voice.Evaluate(0, default), Has.Count.EqualTo(1));
            Assert.That(voice.IsFinished(0.5), Is.False);
            voice.Release(0.5);
            Assert.That(voice.IsFinished(1.5), Is.True);
        }
    }
}
