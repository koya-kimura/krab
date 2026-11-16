using System;
using Krab.TypeLayout.Core.Distribution;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using NUnit.Framework;
using static Krab.TypeLayout.Tests.Editor.GeometryTests;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class DistributionTests
    {
        private static readonly LinePath Line = new(new Point2(100, 50), new Point2(1820, 50));

        [Test]
        public void EndsPutFirstAndLastSlotsExactlyOnOpenPathEnds()
        {
            var slots = SlotDistributor.Distribute(Line, 5, SlotSpacing.Ends);
            Assert.That(slots[0].Position, Is.EqualTo(new Point2(100, 50)));
            Assert.That(slots[2].Position, Is.EqualTo(new Point2(960, 50)));
            Assert.That(slots[4].Position, Is.EqualTo(new Point2(1820, 50)));
            var single = SlotDistributor.Distribute(Line, 1, SlotSpacing.Ends);
            Assert.That(single[0].Position, Is.EqualTo(new Point2(960, 50)));
        }

        [Test]
        public void CellsCenterEachSlotInEqualCellsForOddAndEvenCounts()
        {
            var even = SlotDistributor.Distribute(Line, 4, SlotSpacing.Cells);
            Assert.That(even[0].Position.X, Is.EqualTo(315));
            Assert.That(even[3].Position.X, Is.EqualTo(1605));
            Assert.That(even[1].Position.X + even[2].Position.X, Is.EqualTo(1920));

            var odd = SlotDistributor.Distribute(Line, 3, SlotSpacing.Cells);
            Assert.That(odd[1].Position.X, Is.EqualTo(960));
        }

        [Test]
        public void ClosedPathsNeverDuplicateTheSeamAndSpaceEvenly()
        {
            var circle = ArcPath.Circle(new Point2(0, 0), 100);
            var slots = SlotDistributor.Distribute(circle, 4, SlotSpacing.Ends);
            AssertPoint(slots[0].Position, 0, -100);
            AssertPoint(slots[1].Position, 100, 0);
            AssertPoint(slots[2].Position, 0, 100);
            AssertPoint(slots[3].Position, -100, 0);

            var cells = SlotDistributor.Distribute(circle, 4, SlotSpacing.Cells);
            Assert.That(cells[0].DirectionDegrees, Is.EqualTo(45).Within(1e-9));

            var perimeter = new RectPerimeterPath(new Rect2(0, 0, 300, 100));
            var corners = SlotDistributor.Distribute(perimeter, 8, SlotSpacing.Ends);
            Assert.That(corners[4].Position, Is.EqualTo(new Point2(300, 100)));
            Assert.That(corners[4].DirectionDegrees, Is.EqualTo(180));
        }

        [Test]
        public void SlotsRejectInvalidCountsAndIndices()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SlotDistributor.Distribute(Line, 0, SlotSpacing.Cells));
            Assert.Throws<ArgumentNullException>(() => SlotDistributor.Distribute(null, 3, SlotSpacing.Cells));
            Assert.Throws<ArgumentOutOfRangeException>(() => SlotDistributor.Distance(Line, 3, SlotSpacing.Ends, 3));
        }

        [Test]
        public void GridCentersCellsAndCoversTheAreaWithoutGaps()
        {
            var area = new Rect2(0, 0, 1920, 1080);
            var slots = GridDistributor.Distribute(area, 10, 6);
            Assert.That(slots.Length, Is.EqualTo(60));
            Assert.That(slots[0].Position, Is.EqualTo(new Point2(96, 90)));
            Assert.That(slots[9].Position, Is.EqualTo(new Point2(1824, 90)));
            Assert.That(slots[59].Position, Is.EqualTo(new Point2(1824, 990)));
            Assert.That(slots[0].DirectionDegrees, Is.EqualTo(0));

            var columnMajor = GridDistributor.Distribute(area, 10, 6, GridOrder.ColumnMajor);
            Assert.That(columnMajor[1].Position, Is.EqualTo(new Point2(96, 270)));
            Assert.That(columnMajor[6].Position, Is.EqualTo(new Point2(288, 90)));

            Assert.That(GridDistributor.Cell(area, 10, 6, 9, 5), Is.EqualTo(Rect2.FromEdges(1728, 900, 1920, 1080)));
            Assert.That(GridDistributor.Cell(new Rect2(0, 0, 100, 10), 3, 1, 2, 0).Right, Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridDistributor.Cell(area, 10, 6, 10, 0));
        }
    }
}
