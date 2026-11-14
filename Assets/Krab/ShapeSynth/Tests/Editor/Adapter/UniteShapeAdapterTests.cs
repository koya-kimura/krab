using System;
using System.Collections.Generic;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Evaluation;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Runtime;
using Krab.ShapeSynth.Unity.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class UniteShapeAdapterTests
    {
        [Test]
        public void TargetMatrixCentersFullAffineAndCanInvertTargetY()
        {
            var transform = new Affine2D(2, 3, 5, 7, 11, 13);
            var normal = UniteShapeAdapter.CreateTargetMatrix(transform, 640, 480);
            var inverted = UniteShapeAdapter.CreateTargetMatrix(transform, 640, 480, true);

            AssertPoint(normal, new Vector2(1, 2), 333, 282);
            AssertPoint(inverted, new Vector2(1, 2), 333, 198);
            Assert.That(normal.m00, Is.EqualTo(2));
            Assert.That(normal.m01, Is.EqualTo(3));
            Assert.That(normal.m10, Is.EqualTo(7));
            Assert.That(normal.m11, Is.EqualTo(11));
        }

        [TestCase(0, 100)]
        [TestCase(-1, 100)]
        [TestCase(float.NaN, 100)]
        [TestCase(100, float.PositiveInfinity)]
        public void TargetMatrixRejectsInvalidViewport(float width, float height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                UniteShapeAdapter.CreateTargetMatrix(Affine2D.Identity, width, height));
        }

        [Test]
        public void NonPositiveSizedShapesAreSkippedBeforeFloatConversion()
        {
            var gameObject = new GameObject("Adapter test canvas");
            try
            {
                var canvas = gameObject.AddComponent<Unite.Canvas.UCanvas>();
                var shape = ShapeDefinition.Circle(ParameterDefinition.Constant(-1),
                    new TransformDefinition(positionX: ParameterDefinition.Constant(double.MaxValue)));
                var items = Evaluate(shape);
                Assert.DoesNotThrow(() => UniteShapeAdapter.Draw(canvas, items, 640, 480));
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); }
        }

        [Test]
        public void PolygonFloatOverflowFailsBeforeDrawingBegins()
        {
            var gameObject = new GameObject("Adapter test canvas");
            try
            {
                var canvas = gameObject.AddComponent<Unite.Canvas.UCanvas>();
                var polygon = ShapeDefinition.Polygon(new[]
                {
                    new Point2(0, 0), new Point2(1, 0), new Point2(0, 1e100)
                });
                var items = Evaluate(polygon);
                Assert.Throws<ArgumentOutOfRangeException>(() => UniteShapeAdapter.Draw(canvas, items, 640, 480));
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); }
        }

        private static IReadOnlyList<DrawItem> Evaluate(ShapeDefinition shape) =>
            new HierarchyEvaluator(new GroupDefinition(null, shape), Array.Empty<IModulator>())
                .Evaluate(0, new ReferenceContext(640, 480));

        private static void AssertPoint(Matrix4x4 matrix, Vector2 point, float x, float y)
        {
            var result = matrix.MultiplyPoint3x4(point);
            Assert.That(result.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(y).Within(0.0001f));
        }
    }
}
