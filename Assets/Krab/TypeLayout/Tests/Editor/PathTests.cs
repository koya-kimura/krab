using System;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using NUnit.Framework;
using static Krab.TypeLayout.Tests.Editor.GeometryTests;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class PathTests
    {
        [Test]
        public void LineSamplesByArcLengthAndHitsTheEndExactly()
        {
            var line = new LinePath(new Point2(0, 0), new Point2(30, 40));
            Assert.That(line.Length, Is.EqualTo(50));
            AssertPoint(line.Sample(25).Position, 15, 20);
            Assert.That(line.Sample(50).Position, Is.EqualTo(new Point2(30, 40)));
            Assert.That(line.Sample(0).DirectionDegrees, Is.EqualTo(Math.Atan2(40, 30) * 180 / Math.PI).Within(1e-12));

            // Open paths continue straight past their ends.
            AssertPoint(line.Sample(-10).Position, -6, -8);
            Assert.Throws<ArgumentException>(() => new LinePath(new Point2(1, 1), new Point2(1, 1)));
        }

        [Test]
        public void EdgesReadClockwiseWithExactAxisDirections()
        {
            var rect = new Rect2(40, 40, 1840, 1000);
            AssertEdge(LinePath.AlongEdge(rect, RectEdge.Top), 40, 40, 1880, 40, 0);
            AssertEdge(LinePath.AlongEdge(rect, RectEdge.Right), 1880, 40, 1880, 1040, 90);
            AssertEdge(LinePath.AlongEdge(rect, RectEdge.Bottom), 1880, 1040, 40, 1040, 180);
            AssertEdge(LinePath.AlongEdge(rect, RectEdge.Left), 40, 1040, 40, 40, -90);
        }

        [Test]
        public void CircleStartsAtTopRunsClockwiseAndWrapsTheSeam()
        {
            var circle = ArcPath.Circle(new Point2(960, 540), 300);
            Assert.That(circle.IsClosed, Is.True);
            Assert.That(circle.Length, Is.EqualTo(600 * Math.PI).Within(1e-9));

            var top = circle.Sample(0);
            Assert.That(top.Position, Is.EqualTo(new Point2(960, 240)));
            Assert.That(top.DirectionDegrees, Is.EqualTo(0));

            var quarter = circle.Sample(circle.Length / 4);
            AssertPoint(quarter.Position, 1260, 540);
            Assert.That(quarter.DirectionDegrees, Is.EqualTo(90).Within(1e-9));

            AssertPoint(circle.Sample(circle.Length).Position, 960, 240);
            AssertPoint(circle.Sample(-circle.Length / 4).Position, 660, 540);
        }

        [Test]
        public void CounterClockwiseArcTurnsTheOtherWay()
        {
            var circle = ArcPath.Circle(new Point2(0, 0), 10, clockwise: false);
            var top = circle.Sample(0);
            Assert.That(top.DirectionDegrees, Is.EqualTo(180));
            AssertPoint(circle.Sample(circle.Length / 4).Position, -10, 0);

            var half = new ArcPath(new Point2(0, 0), 10, 180, 180);
            Assert.That(half.IsClosed, Is.False);
            AssertPoint(half.Sample(half.Length).Position, 10, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArcPath(new Point2(0, 0), 10, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArcPath(new Point2(0, 0), 10, 0, 361));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArcPath(new Point2(0, 0), 0, 0, 90));
        }

        [Test]
        public void PerimeterVisitsCornersAtEdgeLengthsAndCornersBelongToTheNextEdge()
        {
            var perimeter = new RectPerimeterPath(new Rect2(0, 0, 200, 100));
            Assert.That(perimeter.Length, Is.EqualTo(600));

            AssertSlot(perimeter.Sample(0), 0, 0, 0);
            AssertSlot(perimeter.Sample(200), 200, 0, 90);
            AssertSlot(perimeter.Sample(300), 200, 100, 180);
            AssertSlot(perimeter.Sample(500), 0, 100, -90);
            AssertSlot(perimeter.Sample(600), 0, 0, 0);
            AssertSlot(perimeter.Sample(-50), 0, 50, -90);
            AssertSlot(perimeter.Sample(199.5), 199.5, 0, 0);
        }

        [Test]
        public void PerimeterCanStartAtAnyCornerInEitherDirection()
        {
            var rect = new Rect2(0, 0, 200, 100);
            var counter = new RectPerimeterPath(rect, Anchor.BottomRight, clockwise: false);
            AssertSlot(counter.Sample(0), 200, 100, -90);
            AssertSlot(counter.Sample(100), 200, 0, 180);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RectPerimeterPath(rect, Anchor.Center));
            Assert.Throws<ArgumentException>(() => new RectPerimeterPath(new Rect2(0, 0, 0, 10)));
        }

        private static void AssertEdge(LinePath edge, double x0, double y0, double x1, double y1, double degrees)
        {
            Assert.That(edge.Sample(0).Position, Is.EqualTo(new Point2(x0, y0)));
            Assert.That(edge.Sample(edge.Length).Position, Is.EqualTo(new Point2(x1, y1)));
            Assert.That(edge.Sample(0).DirectionDegrees, Is.EqualTo(degrees));
        }

        private static void AssertSlot(LayoutSlot slot, double x, double y, double degrees)
        {
            Assert.That(slot.Position, Is.EqualTo(new Point2(x, y)));
            Assert.That(slot.DirectionDegrees, Is.EqualTo(degrees));
        }
    }
}
