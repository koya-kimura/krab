using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Geometry;
using Krab.ShapeSynth.Unity.Rendering;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor.Adapter
{
    public sealed class StrokeWidthTests
    {
        [Test]
        public void PolygonUniformScaleAndReflectionMatchLocalStrokeUnits()
        {
            var matrix = Affine2D.Trs(50, 10, 30, -2, 2);

            Assert.That(
                UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Polygon, 4, matrix), Is.EqualTo(8).Within(1e-6));
            Assert.That(UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Circle, 4, matrix), Is.EqualTo(4));
            Assert.That(UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Rectangle, 4, matrix), Is.EqualTo(4));
        }

        [Test]
        public void PolygonNonuniformScaleUsesGeometricMeanAndCollapsedAreaHasZeroWidth()
        {
            Assert.That(
                UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Polygon, 4, Affine2D.Trs(0, 0, 0, 4, 1)),
                Is.EqualTo(8));
            Assert.That(
                UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Polygon, 4, Affine2D.Trs(0, 0, 0, 0, 1)),
                Is.Zero);
        }

        [Test]
        public void InvalidOrOverflowingSubmittedWidthsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Circle, -1, Affine2D.Identity));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => UniteShapeAdapter.ResolveStrokeWidth(ShapeKind.Circle, double.NaN, Affine2D.Identity));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => UniteShapeAdapter.ResolveStrokeWidth(
                    ShapeKind.Polygon, double.MaxValue, Affine2D.Trs(0, 0, 0, 2, 2)));
        }
    }
}
