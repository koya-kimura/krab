using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class DrawDataTests
    {
        [Test]
        public void EvaluatedItemsContainOrderedShapeDataAcrossNestedAndEmptyGroups()
        {
            var circle = ShapeDefinition.Circle(
                ParameterDefinition.Constant(4), opacity: ParameterDefinition.Constant(-0.25),
                color: new RgbaColor(0.1, 0.2, 0.3, 0.4));
            var rectangle = ShapeDefinition.Rectangle(ParameterDefinition.Constant(8), ParameterDefinition.Constant(6),
                opacity: ParameterDefinition.Constant(1.5), color: new RgbaColor(0.5, 0.6, 0.7));
            var polygon = ShapeDefinition.Polygon(new[] { new Point2(-2, 1), new Point2(3, 1), new Point2(0, 5) },
                color: new RgbaColor(0.8, 0.7, 0.6));
            var root = new GroupDefinition(
                null, circle, new GroupDefinition(null, rectangle, new GroupDefinition(), polygon));

            var items = new HierarchyEvaluator(root, Array.Empty<IModulator>())
                .Evaluate(0, new ReferenceContext(800, 450));

            Assert.That(items, Has.Count.EqualTo(3));
            Assert.That(items[0].Kind, Is.EqualTo(ShapeKind.Circle));
            AssertColor(items[0].FillColor, 0.1, 0.2, 0.3, 0.4);
            Assert.That(items[0].Opacity, Is.EqualTo(-0.25));
            Assert.That(items[0].Vertices, Is.Empty);
            Assert.That(items[1].Kind, Is.EqualTo(ShapeKind.Rectangle));
            Assert.That(items[1].Width, Is.EqualTo(8));
            Assert.That(items[1].Height, Is.EqualTo(6));
            Assert.That(items[1].Opacity, Is.EqualTo(1.5));
            Assert.That(items[2].Kind, Is.EqualTo(ShapeKind.Polygon));
            AssertColor(items[2].FillColor, 0.8, 0.7, 0.6, 1);
            Assert.That(items[2].Vertices, Has.Count.EqualTo(3));
            AssertPoint(items[2].Vertices[0], -2, 1);
            AssertPoint(items[2].Vertices[1], 3, 1);
            AssertPoint(items[2].Vertices[2], 0, 5);
        }

        [Test]
        public void EvaluatedDimensionsFollowCurrentReferenceContext()
        {
            var width = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasWidth, 0.1), 1);
            var height = new ParameterDefinition(new ReferenceDefinition(ReferenceSource.CanvasHeight, 0.2), 1);
            var evaluator = new HierarchyEvaluator(new GroupDefinition(null,
                ShapeDefinition.Rectangle(width, height)), Array.Empty<IModulator>());

            var small = evaluator.Evaluate(0, new ReferenceContext(100, 50))[0];
            var large = evaluator.Evaluate(0, new ReferenceContext(300, 200))[0];

            Assert.That(small.Width, Is.EqualTo(10));
            Assert.That(small.Height, Is.EqualTo(10));
            Assert.That(large.Width, Is.EqualTo(30));
            Assert.That(large.Height, Is.EqualTo(40));
        }

        [Test]
        public void RepeatedEvaluationProducesTheSameDrawData()
        {
            var definition = new GroupDefinition(new TransformDefinition(positionX: ParameterDefinition.Constant(12)),
                ShapeDefinition.Polygon(new[] { new Point2(0, 0), new Point2(2, 0), new Point2(1, 3) },
                    opacity: ParameterDefinition.Constant(0.75), color: new RgbaColor(0.2, 0.4, 0.6)));
            var evaluator = new HierarchyEvaluator(definition, Array.Empty<IModulator>());

            var first = evaluator.Evaluate(2, new ReferenceContext(800, 450))[0];
            var second = evaluator.Evaluate(2, new ReferenceContext(800, 450))[0];

            Assert.That(second.Kind, Is.EqualTo(first.Kind));
            AssertColor(second.FillColor, first.FillColor.R, first.FillColor.G, first.FillColor.B, first.FillColor.A);
            Assert.That(second.Width, Is.EqualTo(first.Width));
            Assert.That(second.Height, Is.EqualTo(first.Height));
            Assert.That(second.Opacity, Is.EqualTo(first.Opacity));
            AssertPoint(second.WorldTransform.TransformPoint(new Point2(0, 0)), 12, 0);
            AssertPoint(first.Vertices[2], second.Vertices[2].X, second.Vertices[2].Y);
        }

        private static void AssertColor(RgbaColor actual, double r, double g, double b, double a)
        {
            Assert.That(actual.R, Is.EqualTo(r));
            Assert.That(actual.G, Is.EqualTo(g));
            Assert.That(actual.B, Is.EqualTo(b));
            Assert.That(actual.A, Is.EqualTo(a));
        }

        private static void AssertPoint(Point2 actual, double x, double y)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
        }
    }
}
