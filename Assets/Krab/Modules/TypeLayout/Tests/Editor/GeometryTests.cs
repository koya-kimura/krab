using System;
using Krab.TypeLayout.Core.Geometry;
using NUnit.Framework;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class GeometryTests
    {
        [Test]
        public void AnchorsHitCornersEdgesAndCenterExactly()
        {
            var rect = new Rect2(10, 20, 300, 100);
            AssertPoint(rect.PointAt(Anchor.TopLeft), 10, 20);
            AssertPoint(rect.PointAt(Anchor.Top), 160, 20);
            AssertPoint(rect.PointAt(Anchor.TopRight), 310, 20);
            AssertPoint(rect.PointAt(Anchor.Left), 10, 70);
            AssertPoint(rect.PointAt(Anchor.Center), 160, 70);
            AssertPoint(rect.PointAt(Anchor.BottomRight), 310, 120);
            AssertPoint(rect.Center, 160, 70);
        }

        [Test]
        public void PlaceAlignsABoxFlushToTheAnchoredSides()
        {
            var rect = new Rect2(0, 0, 1920, 1080);
            Assert.That(rect.Place(100, 40, Anchor.BottomRight), Is.EqualTo(new Rect2(1820, 1040, 100, 40)));
            Assert.That(rect.Place(100, 40, Anchor.Center), Is.EqualTo(new Rect2(910, 520, 100, 40)));
            Assert.That(rect.Place(100, 40, Anchor.TopLeft), Is.EqualTo(new Rect2(0, 0, 100, 40)));
        }

        [Test]
        public void InsetShrinksEachSideAndRejectsInsetsLargerThanTheRect()
        {
            var rect = new Rect2(0, 0, 100, 50);
            Assert.That(rect.Inset(10, 5, 20, 15), Is.EqualTo(Rect2.FromEdges(10, 5, 80, 35)));
            Assert.That(rect.Inset(25), Is.EqualTo(Rect2.FromEdges(25, 25, 75, 25)));
            Assert.Throws<ArgumentOutOfRangeException>(() => rect.Inset(26));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rect2(0, 0, -1, 10));
        }

        [Test]
        public void FrameScalesReferencePixelsWithOutputHeight()
        {
            var full = new LayoutFrame(1920, 1080);
            var half = new LayoutFrame(960, 540);
            Assert.That(full.Unit, Is.EqualTo(1));
            Assert.That(half.ToPixels(80), Is.EqualTo(40));
            Assert.That(half.SafeArea(80), Is.EqualTo(Rect2.FromEdges(40, 40, 920, 500)));

            // Wider output keeps the same unit; only the horizontal room grows.
            Assert.That(new LayoutFrame(2560, 1080).Unit, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutFrame(0, 1080));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutFrame(1920, 1080, double.NaN));
        }

        [Test]
        public void RightAnglesAreExactAndClockwiseOnScreen()
        {
            Assert.That(Angles.CosSin(90), Is.EqualTo((0.0, 1.0)));
            Assert.That(Angles.CosSin(-90), Is.EqualTo((0.0, -1.0)));
            Assert.That(Angles.CosSin(540), Is.EqualTo((-1.0, 0.0)));
            AssertPoint(new Point2(10, 0).Rotate(90), 0, 10);
            Assert.That(Angles.Normalize(270), Is.EqualTo(-90));
            Assert.That(Angles.Normalize(-180), Is.EqualTo(180));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Point2(double.PositiveInfinity, 0));
        }

        internal static void AssertPoint(Point2 actual, double x, double y, double tolerance = 1e-9)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(tolerance), "X");
            Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance), "Y");
        }
    }
}
