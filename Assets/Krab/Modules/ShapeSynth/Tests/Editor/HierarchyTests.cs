using System;
using System.Collections.Generic;
using System.Reflection;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class HierarchyTests
    {
        private static readonly ReferenceContext Context = new(800, 450);

        [Test]
        public void IdentityTranslationRotationAndParentChildCompositionAreAnalytic()
        {
            var point = new Point2(2, 3);
            AssertPoint(Affine2D.Identity.TransformPoint(point), 2, 3);
            AssertPoint(Affine2D.Trs(5, -2, 0, 1, 1).TransformPoint(point), 7, 1);
            AssertPoint(Affine2D.Trs(0, 0, 90, 1, 1).TransformPoint(point), -3, 2);

            var parent = Affine2D.Trs(10, 20, 90, 1, 1);
            var child = Affine2D.Trs(2, 0, 0, 1, 1);
            AssertPoint((parent * child).TransformPoint(new Point2(1, 0)), 10, 23);

            // Three levels: root translation, a rotated group, then a translated shape.
            var hierarchy = new GroupDefinition(new TransformDefinition(positionX: Constant(100)),
                new GroupDefinition(new TransformDefinition(rotationDegrees: Constant(90)),
                    ShapeDefinition.Circle(Constant(4), new TransformDefinition(positionX: Constant(50)))));
            var item = Evaluate(hierarchy)[0];
            AssertPoint(item.WorldTransform.TransformPoint(new Point2(0, 0)), 100, 50);

            var reversed = Affine2D.Trs(50, 0, 0, 1, 1) * Affine2D.Trs(100, 0, 90, 1, 1);
            AssertPoint(reversed.TransformPoint(new Point2(0, 0)), 150, 0);
            Assert.That(reversed.TransformPoint(new Point2(0, 0)).X, Is.Not.EqualTo(100).Within(1e-10));
        }

        [Test]
        public void TrsScalesThenRotatesAndSupportsNegativeScaleAndShearComposition()
        {
            AssertPoint(Affine2D.Trs(3, 4, 90, 2, 3).TransformPoint(new Point2(1, 1)), 0, 6);
            AssertPoint(Affine2D.Trs(0, 0, 0, -2, 3).TransformPoint(new Point2(1, 2)), -2, 6);

            // Non-uniform parent scale composed with a rotated child produces shear.
            var sheared = Affine2D.Trs(0, 0, 0, 2, 1) * Affine2D.Trs(0, 0, 45, 1, 1);
            AssertPoint(sheared.TransformPoint(new Point2(1, 0)), Math.Sqrt(2), Math.Sqrt(0.5));
            AssertPoint(sheared.TransformPoint(new Point2(0, 1)), -Math.Sqrt(2), Math.Sqrt(0.5));
        }

        [Test]
        public void EmptyAndMultipleGroupsPreserveDepthFirstShapeOrder()
        {
            var empty = new GroupDefinition();
            Assert.That(Evaluate(empty), Is.Empty);

            var first = ShapeDefinition.Circle(Constant(2));
            var second = ShapeDefinition.Rectangle(Constant(3), Constant(4));
            var nested = new GroupDefinition(null, first);
            var root = new GroupDefinition(null, nested, second, new GroupDefinition());
            var items = Evaluate(root);
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Kind, Is.EqualTo(ShapeKind.Circle));
            Assert.That(items[0].Width, Is.EqualTo(2));
            Assert.That(items[1].Kind, Is.EqualTo(ShapeKind.Rectangle));
            Assert.That(items[1].Width, Is.EqualTo(3));
            Assert.That(items[1].Height, Is.EqualTo(4));
        }

        [Test]
        public void SharedDefinitionEvaluatesSeparatelyForEachParentOccurrence()
        {
            var shared = ShapeDefinition.Circle(Constant(2));
            var sharedGroup = new GroupDefinition(null, shared);
            var leftParent = new GroupDefinition(new TransformDefinition(positionX: Constant(10)), sharedGroup);
            var rightParent = new GroupDefinition(new TransformDefinition(positionY: Constant(20)), sharedGroup);
            var items = Evaluate(new GroupDefinition(null, leftParent, rightParent));

            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Kind, Is.EqualTo(ShapeKind.Circle));
            Assert.That(items[1].Kind, Is.EqualTo(ShapeKind.Circle));
            Assert.That(items[0].Width, Is.EqualTo(2));
            Assert.That(items[1].Width, Is.EqualTo(2));
            AssertPoint(items[0].WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);
            AssertPoint(items[1].WorldTransform.TransformPoint(new Point2(0, 0)), 0, 20);
        }

        [Test]
        public void SizesDoNotMoveTheLocalOriginAndPolygonVerticesRemainLocal()
        {
            var transform = new TransformDefinition(positionX: Constant(7), positionY: Constant(-3));
            var circle = ShapeDefinition.Circle(Constant(12), transform);
            var rectangle = ShapeDefinition.Rectangle(Constant(8), Constant(6), transform);
            var polygon = ShapeDefinition.Polygon(
                new[] { new Point2(1, 2), new Point2(-1, 0), new Point2(0, 3) }, transform);
            var items = Evaluate(new GroupDefinition(null, circle, rectangle, polygon));

            foreach (var item in items)
                AssertPoint(item.WorldTransform.TransformPoint(new Point2(0, 0)), 7, -3);
            Assert.That(items[0].Width, Is.EqualTo(12));
            Assert.That(items[0].Height, Is.EqualTo(12));
            Assert.That(items[1].Width, Is.EqualTo(8));
            Assert.That(items[1].Height, Is.EqualTo(6));
            Assert.That(items[2].Vertices, Has.Count.EqualTo(3));
            AssertPoint(items[2].Vertices[0], 1, 2);
            AssertPoint(items[2].WorldTransform.TransformPoint(items[2].Vertices[0]), 8, -1);

            // An asymmetric child with large bounds still leaves its group's pivot at the origin.
            var offCenterChild = ShapeDefinition.Rectangle(Constant(40), Constant(12),
                new TransformDefinition(positionX: Constant(25), positionY: Constant(-7)));
            var pivotGroup = new GroupDefinition(
                new TransformDefinition(positionX: Constant(3), positionY: Constant(5)), offCenterChild);
            AssertPoint(Evaluate(pivotGroup)[0].WorldTransform.TransformPoint(new Point2(0, 0)), 28, -2);
        }

        [Test]
        public void AdsrAndLfoBindingsAnimatePositionRotationAndDimensions()
        {
            var adsr = new AdsrInstance(new AdsrDefinition(1, 0, 1, 0, 1), new VoiceTime(0, 60), false);
            var lfo = new LfoInstance(new LfoDefinition(0.25, 0, LfoWaveform.Sine), new VoiceTime(0, 60));
            var modulators = new IModulator[] { adsr, lfo };
            var moving = new TransformDefinition(
                positionX: Bound(0, 10),
                rotationDegrees: Bound(1, 90));
            var shape = ShapeDefinition.Rectangle(Bound(0, 4), Constant(2), moving);
            var evaluator = new HierarchyEvaluator(new GroupDefinition(null, shape), modulators);

            var atZero = evaluator.Evaluate(0, Context)[0];
            var atOne = evaluator.Evaluate(1, Context)[0];
            Assert.That(atZero.Width, Is.EqualTo(0).Within(1e-12));
            Assert.That(atOne.Width, Is.EqualTo(4).Within(1e-12));
            AssertPoint(atZero.WorldTransform.TransformPoint(new Point2(0, 0)), 0, 0);
            AssertPoint(atOne.WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);
            AssertPoint(atOne.WorldTransform.TransformPoint(new Point2(1, 0)), 10, 1);
        }

        [Test]
        public void RuntimeUsesProvidedSharedModulatorInstancesWithoutCloningThem()
        {
            var sharedSignal = new LfoInstance(new LfoDefinition(0.25), new VoiceTime(0, 60));
            var shape = ShapeDefinition.Circle(Bound(0, 2));
            var evaluator = new HierarchyEvaluator(
                new GroupDefinition(null, shape, shape), new IModulator[] { sharedSignal });

            var items = evaluator.Evaluate(1, Context);

            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Width, Is.EqualTo(items[1].Width).Within(1e-12));
            Assert.That(items[0].Width, Is.EqualTo(2).Within(1e-12));
        }

        [Test]
        public void GroupParameterBindingsCombineAndReferencesUseCurrentCanvasContext()
        {
            var lfo = new LfoInstance(new LfoDefinition(0.25), new VoiceTime(0, 60));
            var adsr = new AdsrInstance(new AdsrDefinition(1, 0, 1, 10, 1), new VoiceTime(0, 60), false);
            var x = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth, 0.5), 0,
                new BindingDefinition(0, 10), new BindingDefinition(1, 2));
            var group = new GroupDefinition(new TransformDefinition(positionX: x), ShapeDefinition.Circle(Constant(2)));
            var evaluator = new HierarchyEvaluator(group, new IModulator[] { lfo, adsr });

            // At t=1 both signals are 1: (10 + 2) * canvasWidth * 0.5.
            AssertPoint(evaluator.Evaluate(1, new ReferenceContext(800, 450))[0]
                .WorldTransform.TransformPoint(new Point2(0, 0)), 4800, 0);
            AssertPoint(evaluator.Evaluate(1, new ReferenceContext(200, 450))[0]
                .WorldTransform.TransformPoint(new Point2(0, 0)), 1200, 0);
        }

        [Test]
        public void SharedTreeHasIndependentEvaluatorSignalStateAndDrawItemsAreSnapshots()
        {
            var envelopeDefinition = new AdsrDefinition(0, 0, 1, 10, 1);
            var sharedTree = new GroupDefinition(new TransformDefinition(positionX: Bound(0, 10)),
                ShapeDefinition.Circle(Constant(2)));
            var releasedSignal = new AdsrInstance(envelopeDefinition, new VoiceTime(0, 60), false);
            var heldSignal = new AdsrInstance(envelopeDefinition, new VoiceTime(0, 60), false);
            var releasedEvaluator = new HierarchyEvaluator(sharedTree, new IModulator[] { releasedSignal });
            var heldEvaluator = new HierarchyEvaluator(sharedTree, new IModulator[] { heldSignal });

            var oldItem = releasedEvaluator.Evaluate(0.5, Context)[0];
            releasedSignal.Release(0.5);
            var repeated = releasedEvaluator.Evaluate(0.5, Context)[0];
            AssertPoint(repeated.WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);
            AssertPoint(oldItem.WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);

            AssertPoint(
                releasedEvaluator.Evaluate(1, Context)[0].WorldTransform.TransformPoint(new Point2(0, 0)), 5, 0);
            AssertPoint(
                heldEvaluator.Evaluate(1, Context)[0].WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);
            AssertPoint(oldItem.WorldTransform.TransformPoint(new Point2(0, 0)), 10, 0);
        }

        [Test]
        public void ImmutableConstructorsRejectNullChildrenAndDoNotRetainCallerArray()
        {
            var shape = ShapeDefinition.Circle(Constant(1));
            INodeDefinition[] inputChildren = { shape };
            var group = new GroupDefinition(null, inputChildren);
            inputChildren[0] = null;

            Assert.That(group.Children, Has.Count.EqualTo(1));
            Assert.That(group.Children[0], Is.SameAs(shape));
            Assert.Throws<ArgumentException>(() => new GroupDefinition(null, new INodeDefinition[] { null }));
            Assert.Throws<ArgumentNullException>(() => new GroupDefinition(null, (INodeDefinition[])null));

            // Public construction defensively copies the array, so a normal caller cannot form a cycle.
            INodeDefinition[] recursiveArray = { shape };
            var apparentlyRecursive = new GroupDefinition(null, recursiveArray);
            recursiveArray[0] = apparentlyRecursive;
            Assert.That(apparentlyRecursive.Children[0], Is.SameAs(shape));
        }

        [Test]
        public void NonFinitePointsAndAffineInputsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Point2(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Point2(0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Affine2D(1, 0, 0, 0, double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Affine2D.Trs(0, 0, double.PositiveInfinity, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Affine2D.Trs(0, 0, 0, double.NaN, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Affine2D.Trs(double.PositiveInfinity, 0, 0, 1, 1));
        }

        [Test]
        public void CorruptCycleIsRejectedAndPolygonInputIsSnapshotted()
        {
            var vertices = new[] { new Point2(-1, -1), new Point2(1, -1), new Point2(0, 2) };
            var polygon = ShapeDefinition.Polygon(vertices);
            vertices[0] = new Point2(999, 999);
            AssertPoint(polygon.Vertices[0], -1, -1);
            Assert.Throws<ArgumentException>(() => ShapeDefinition.Polygon(new[] { new Point2(0, 0) }));

            // Public constructors prevent cycles; simulate corrupt deserialization for this guard test only.
            var group = new GroupDefinition();
            var childrenField = typeof(GroupDefinition).GetField("<Children>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            childrenField.SetValue(group, Array.AsReadOnly<INodeDefinition>(new INodeDefinition[] { group }));
            Assert.Throws<ArgumentException>(() => new HierarchyEvaluator(group, Array.Empty<IModulator>()));
        }

        private static IReadOnlyList<DrawItem> Evaluate(GroupDefinition definition)
        {
            return new HierarchyEvaluator(definition, Array.Empty<IModulator>()).Evaluate(0, Context);
        }

        private static ParameterDefinition Constant(double value) => ParameterDefinition.Constant(value);

        private static ParameterDefinition Bound(int index, double depth)
        {
            return new ParameterDefinition(
                new ReferenceDefinition(ReferenceSource.Absolute), 0, new BindingDefinition(index, depth));
        }

        private static void AssertPoint(Point2 actual, double x, double y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(1e-10));
            Assert.That(actual.Y, Is.EqualTo(y).Within(1e-10));
        }
    }
}
