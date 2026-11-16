using System;
using System.Collections.Generic;
using System.Linq;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Text;
using NUnit.Framework;
using static Krab.TypeLayout.Tests.Editor.GeometryTests;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class FlowTests
    {
        private static readonly FakeMetrics Metrics = new();
        private static readonly LinePath Line = new(new Point2(0, 100), new Point2(1000, 100));
        private readonly List<FlowItem> _items = new();

        [Test]
        public void StartPlacesBoxCentersByAccumulatedAdvanceAndTracking()
        {
            // "Mie" at size 100 → advances 80, 25, 50.
            FlowDistributor.Flow(Line, Metrics.Advances("Mie", 100), new FlowOptions(tracking: 10), _items);
            Assert.That(_items.Select(item => item.Distance), Is.EqualTo(new[] { 40.0, 102.5, 150 }));
            Assert.That(_items[1].Slot.Position, Is.EqualTo(new Point2(102.5, 100)));
            Assert.That(_items.Select(item => item.ClusterIndex), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void CenterAndEndAlignTheWholeRunWithinTheOpenPath()
        {
            var advances = Metrics.Advances("Mie", 100);
            FlowDistributor.Flow(Line, advances, new FlowOptions(tracking: 10, alignment: FlowAlignment.End), _items);
            Assert.That(_items[2].Distance + 25, Is.EqualTo(1000));

            FlowDistributor.Flow(Line, advances, new FlowOptions(tracking: 10, alignment: FlowAlignment.Center), _items);
            var left = _items[0].Distance - 40;
            var right = _items[2].Distance + 25;
            Assert.That(left, Is.EqualTo(1000 - right).Within(1e-9));
        }

        [Test]
        public void JustifyTouchesBothEndsOfAnOpenPath()
        {
            var spacing = FlowDistributor.Flow(Line, Metrics.Advances("Mie", 100),
                new FlowOptions(tracking: 10, alignment: FlowAlignment.Justify), _items);
            Assert.That(spacing, Is.EqualTo((1000 - 155) / 2.0).Within(1e-9));
            Assert.That(_items[0].Distance - 40, Is.EqualTo(0).Within(1e-9));
            Assert.That(_items[2].Distance + 25, Is.EqualTo(1000).Within(1e-9));

            FlowDistributor.Flow(Line, Metrics.Advances("M", 100), new FlowOptions(alignment: FlowAlignment.Justify),
                _items);
            Assert.That(_items[0].Distance, Is.EqualTo(500));
        }

        [Test]
        public void ClipDropsGlyphsThatLeaveAnOpenPathUnlessDisabled()
        {
            var advances = Metrics.Advances("MMMMMMMMMMMMMMM", 100);
            FlowDistributor.Flow(Line, advances, new FlowOptions(), _items);
            Assert.That(_items.Count, Is.EqualTo(12));
            Assert.That(_items.Last().Distance + 40, Is.LessThanOrEqualTo(1000));

            FlowDistributor.Flow(Line, advances, new FlowOptions(keepOverflow: true), _items);
            Assert.That(_items.Count, Is.EqualTo(15));

            // A glyph exactly touching the end is kept.
            FlowDistributor.Flow(Line, Metrics.Advances("M", 100), new FlowOptions(offset: 920), _items);
            Assert.That(_items.Count, Is.EqualTo(1));
            FlowDistributor.Flow(Line, Metrics.Advances("M", 100), new FlowOptions(offset: 921), _items);
            Assert.That(_items, Is.Empty);
        }

        [Test]
        public void ClosedJustifyMakesTheSeamGapEqualToEveryOtherGap()
        {
            var circle = ArcPath.Circle(new Point2(500, 500), 200);
            var advances = Metrics.Advances("Mie", 100);
            var justify = new FlowOptions(alignment: FlowAlignment.Justify);
            var spacing = FlowDistributor.Flow(circle, advances, justify, _items);
            Assert.That(spacing, Is.EqualTo((circle.Length - 155) / 3).Within(1e-9));

            var lastBoxEnd = _items[2].Distance + 25;
            Assert.That(circle.Length - lastBoxEnd, Is.EqualTo(spacing).Within(1e-9));
        }

        [Test]
        public void FillWholeTextRepeatsCompleteWordsAndClosesTheLoopExactly()
        {
            var perimeter = new RectPerimeterPath(new Rect2(0, 0, 1000, 500));
            var advances = Metrics.Advances("Mie ", 100);
            var spacing = FlowDistributor.Flow(perimeter, advances,
                new FlowOptions(tracking: 5, alignment: FlowAlignment.Justify, fill: FlowFill.WholeText), _items);

            // Each repeat takes 185 + 4 * 5 = 205; 3000 / 205 → 14 whole repeats.
            Assert.That(_items.Count, Is.EqualTo(56));
            Assert.That(_items.Last().ClusterIndex, Is.EqualTo(3));
            Assert.That(spacing, Is.GreaterThanOrEqualTo(5));
            Assert.That(perimeter.Length - (_items.Last().Distance + 15), Is.EqualTo(spacing).Within(1e-9));
        }

        [Test]
        public void FillGlyphsKeepsTrackingAndMayStopMidWord()
        {
            var advances = Metrics.Advances("Mie", 100);
            FlowDistributor.Flow(Line, advances, new FlowOptions(tracking: 10, fill: FlowFill.Glyphs), _items);

            // Repeats of 80+25+50 with 10 between: the last box must still end inside the path.
            var last = _items.Last();
            var lastHalf = advances[last.ClusterIndex] / 2;
            Assert.That(last.Distance + lastHalf, Is.LessThanOrEqualTo(1000));
            Assert.That(last.Distance + lastHalf + 10 + advances[(last.ClusterIndex + 1) % 3], Is.GreaterThan(1000));
        }

        [Test]
        public void ClosedPathsAlignAroundTheSeamAndOffsetWraps()
        {
            var circle = ArcPath.Circle(new Point2(0, 0), 100);
            var advances = Metrics.Advances("M", 100);
            FlowDistributor.Flow(circle, advances, new FlowOptions(alignment: FlowAlignment.Center), _items);
            Assert.That(_items[0].Distance, Is.EqualTo(0));
            AssertPoint(_items[0].Slot.Position, 0, -100);

            FlowDistributor.Flow(circle, advances,
                new FlowOptions(alignment: FlowAlignment.Center, offset: circle.Length), _items);
            AssertPoint(_items[0].Slot.Position, 0, -100);
        }

        [Test]
        public void FlowRejectsRepeatingNothingAndHandlesEmptyText()
        {
            Assert.That(FlowDistributor.Flow(Line, Array.Empty<double>(), new FlowOptions(), _items), Is.EqualTo(0));
            Assert.That(_items, Is.Empty);
            Assert.Throws<ArgumentException>(() =>
                FlowDistributor.Flow(Line, new[] { 0.0 }, new FlowOptions(fill: FlowFill.Glyphs), _items));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FlowDistributor.Flow(Line, new[] { -1.0 }, new FlowOptions(), _items));
        }

        [Test]
        public void ClustersKeepSurrogatePairsAndCombiningMarksTogether()
        {
            Assert.That(TextClusters.Split("éM"), Is.EqualTo(new[] { "é", "M" }));
            Assert.That(TextClusters.Split("a\U0001F600"), Is.EqualTo(new[] { "a", "\U0001F600" }));
            Assert.That(TextClusters.Split(null), Is.Empty);

            var results = new List<string>();
            TextSequence.Cycle(new[] { "a", "b", "c" }, 5, -1, results);
            Assert.That(results, Is.EqualTo(new[] { "c", "a", "b", "c", "a" }));
        }
    }
}
