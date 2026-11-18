using System;
using System.Collections.Generic;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    /// <summary>Direction / Distance: a move along an angle on top of Position, without turning the Shape.</summary>
    public sealed class MoveTests
    {
        private static readonly ReferenceContext Canvas = new(100, 100);

        [TestCase(0, 10, 10, 0)]
        [TestCase(90, 10, 0, 10)]
        [TestCase(180, 10, -10, 0)]
        [TestCase(-90, 10, 0, -10)]
        [TestCase(450, 10, 0, 10)]
        [TestCase(90, -10, 0, -10)]
        [TestCase(45, 0, 0, 0)]
        public void DistanceMovesAlongDirectionFromRightCounterclockwise(double direction, double distance,
            double x, double y)
        {
            var transform = new TransformDefinition(Constant(3), Constant(4), directionDegrees: Constant(direction),
                distance: Constant(distance));
            var item = Evaluate(new GroupDefinition(null, ShapeDefinition.Circle(Constant(2), transform)));

            Assert.That(item.WorldTransform.M02, Is.EqualTo(3 + x).Within(1e-9));
            Assert.That(item.WorldTransform.M12, Is.EqualTo(4 + y).Within(1e-9));
        }

        [Test]
        public void MovingDoesNotRotateTheShapeAndKeepsItsOwnRotation()
        {
            var moved = new TransformDefinition(directionDegrees: Constant(60), distance: Constant(5));
            var item = Evaluate(new GroupDefinition(null, ShapeDefinition.Rectangle(Constant(2), Constant(1), moved)));
            Assert.That(item.WorldTransform.M00, Is.EqualTo(1).Within(1e-12));
            Assert.That(item.WorldTransform.M10, Is.EqualTo(0).Within(1e-12));

            var rotated = new TransformDefinition(rotationDegrees: Constant(90), directionDegrees: Constant(0),
                distance: Constant(5));
            item = Evaluate(new GroupDefinition(null, ShapeDefinition.Rectangle(Constant(2), Constant(1), rotated)));
            // The Shape's own Rotation does not turn its move: 0° still goes right.
            Assert.That(item.WorldTransform.M02, Is.EqualTo(5).Within(1e-9));
            Assert.That(item.WorldTransform.M12, Is.EqualTo(0).Within(1e-9));
            Assert.That(item.WorldTransform.M10, Is.EqualTo(1).Within(1e-12));
        }

        [Test]
        public void PresetDistanceFollowsItsBindingAndFlipReversesTheDirection()
        {
            var document = RowOfCircles(4, out var shape);
            shape.Direction.Base = 90;
            shape.Distance.Bind(document.Values[0].Id, 10);
            shape.Distance.Flip = PresetFlip.Alternate;

            var items = Evaluate(document, 1);

            Assert.That(items.Select(item => Math.Round(item.WorldTransform.M12, 9)),
                Is.EqualTo(new[] { 10.0, -10.0, 10.0, -10.0 }));
            Assert.That(items.Select(item => Math.Round(item.WorldTransform.M02, 9)),
                Is.EqualTo(new[] { -15.0, -5.0, 5.0, 15.0 }));
        }

        [Test]
        public void PresetDirectionRandomScattersCopiesAtTheSameDistance()
        {
            var document = RowOfCircles(6, out var shape);
            shape.Layout.SpacingX = new PresetParameter { Base = 0 };
            shape.Direction.Random = 180;
            shape.Distance.Base = 10;

            var items = Evaluate(document, 0, seed: 7);
            var lengths = items.Select(item => Math.Sqrt(item.WorldTransform.M02 * item.WorldTransform.M02
                                                         + item.WorldTransform.M12 * item.WorldTransform.M12));

            Assert.That(lengths, Has.All.EqualTo(10).Within(1e-9));
            Assert.That(items.Select(item => Math.Round(item.WorldTransform.M02, 6)).Distinct().Count(),
                Is.GreaterThan(1));
        }

        [Test]
        public void PresetGroupMoveCarriesItsChildren()
        {
            var document = PresetDocument.CreateNew();
            var group = document.RootGroup;
            group.PositionX.Base = 1;
            group.Direction.Base = 180;
            group.Distance.Base = 4;

            var item = Evaluate(document, 0).Single();

            Assert.That(item.WorldTransform.M02, Is.EqualTo(-3).Within(1e-9));
            Assert.That(item.WorldTransform.M12, Is.EqualTo(0).Within(1e-9));
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

        private static DrawItem Evaluate(GroupDefinition definition)
        {
            return new HierarchyEvaluator(definition, Array.Empty<IModulator>()).Evaluate(0, Canvas).Single();
        }

        private static IReadOnlyList<DrawItem> Evaluate(PresetDocument document, double now, int seed = 0)
        {
            var modulators = PresetCompiler.Components(document).Select(_ => (IModulator)new Clock()).ToArray();
            return new HierarchyEvaluator(PresetCompiler.CompileGroup(document), modulators, seed)
                .Evaluate(now, Canvas);
        }

        private static ParameterDefinition Constant(double value) => ParameterDefinition.Constant(value);

        /// <summary>Signal equal to the time in seconds; one beat per second.</summary>
        private sealed class Clock : IDelayableModulator
        {
            public double Evaluate(double nowSeconds) => nowSeconds;

            public double EvaluateDelayed(double nowSeconds, double delayBeats) => nowSeconds - delayBeats;
        }
    }
}
