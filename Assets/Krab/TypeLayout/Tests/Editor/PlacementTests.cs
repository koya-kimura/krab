using System;
using System.Collections.Generic;
using Krab.TypeLayout.Core.Geometry;
using Krab.TypeLayout.Core.Paths;
using Krab.TypeLayout.Core.Placement;
using NUnit.Framework;
using static Krab.TypeLayout.Tests.Editor.GeometryTests;

namespace Krab.TypeLayout.Tests.Editor
{
    public sealed class PlacementTests
    {
        private static readonly FakeMetrics Metrics = new();
        private static readonly LayoutSlot Right = new(new Point2(500, 300), 0);

        [Test]
        public void AdvanceCenterAndBaselinePutThePenHalfAnAdvanceLeft()
        {
            var placement = GlyphPlacer.Place(Right, "M", Metrics,
                new PlacementOptions(100, vertical: VerticalAnchor.Baseline));
            Assert.That(placement.Origin, Is.EqualTo(new Point2(460, 300)));
            Assert.That(placement.RotationDegrees, Is.EqualTo(0));
            Assert.That(placement.Anchor, Is.EqualTo(Right.Position));
        }

        [Test]
        public void SharedVerticalAnchorsKeepOneBaselineForEveryGlyph()
        {
            var cap = new PlacementOptions(100, vertical: VerticalAnchor.CapCenter);
            Assert.That(GlyphPlacer.Place(Right, "M", Metrics, cap).Origin.Y, Is.EqualTo(335).Within(1e-9));
            Assert.That(GlyphPlacer.Place(Right, "e", Metrics, cap).Origin.Y, Is.EqualTo(335).Within(1e-9));

            var em = new PlacementOptions(100, vertical: VerticalAnchor.EmCenter);
            Assert.That(GlyphPlacer.Place(Right, "g", Metrics, em).Origin.Y, Is.EqualTo(330).Within(1e-9));
        }

        [Test]
        public void InkAnchorsCenterEachDrawnShapeAndFallBackForBlanks()
        {
            var ink = new PlacementOptions(100, HorizontalAnchor.InkCenter, VerticalAnchor.InkCenter);
            var i = GlyphPlacer.Place(Right, "i", Metrics, ink);
            AssertPoint(i.Origin, 500 - 12, 300 + 36);

            var e = GlyphPlacer.Place(Right, "e", Metrics, ink);
            Assert.That(e.Origin.Y, Is.EqualTo(300 + 24.5).Within(1e-9));

            var space = GlyphPlacer.Place(Right, " ", Metrics, ink);
            AssertPoint(space.Origin, 485, 335);
        }

        [Test]
        public void FollowPathRotatesTheAnchorOffsetWithTheGlyph()
        {
            // Text running down the right edge: the advance runs along +Y and "up" points to +X (outward).
            var down = new LayoutSlot(new Point2(1880, 500), 90);
            var placement = GlyphPlacer.Place(down, "M", Metrics, new PlacementOptions(100));
            Assert.That(placement.RotationDegrees, Is.EqualTo(90));
            Assert.That(placement.Origin, Is.EqualTo(new Point2(1880 - 35, 460)));

            // Bottom edge reads right to left, upside down.
            var bottom = new LayoutSlot(new Point2(960, 1040), 180);
            Assert.That(GlyphPlacer.Place(bottom, "M", Metrics, new PlacementOptions(100)).Origin,
                Is.EqualTo(new Point2(1000, 1040 - 35)));
        }

        [Test]
        public void UprightIgnoresThePathAndOffsetTurnsEachGlyph()
        {
            var down = new LayoutSlot(new Point2(100, 100), 90);
            var upright = GlyphPlacer.Place(down, "M", Metrics,
                new PlacementOptions(100, vertical: VerticalAnchor.Baseline, rotation: RotationMode.Upright));
            Assert.That(upright.Origin, Is.EqualTo(new Point2(60, 100)));

            var turned = GlyphPlacer.Place(down, "M", Metrics,
                new PlacementOptions(100, vertical: VerticalAnchor.Baseline, rotationOffsetDegrees: 90));
            Assert.That(turned.RotationDegrees, Is.EqualTo(180));
            Assert.That(turned.Origin, Is.EqualTo(new Point2(140, 100)));
        }

        [Test]
        public void SnapRoundsThePenOriginToWholePixels()
        {
            var slot = new LayoutSlot(new Point2(100.4, 50.6), 0);
            var placement = GlyphPlacer.Place(slot, "i", Metrics,
                new PlacementOptions(10, vertical: VerticalAnchor.Baseline, snapToPixel: true));
            Assert.That(placement.Origin, Is.EqualTo(new Point2(99, 51)));
        }

        [Test]
        public void BulkPlacementPairsSlotsAndTextsInOrder()
        {
            var results = new List<GlyphPlacement>();
            var slots = new[] { Right, new LayoutSlot(new Point2(0, 0), 0) };
            GlyphPlacer.Place(slots, new[] { "M", "e" }, Metrics, new PlacementOptions(100), results);
            Assert.That(results[1].Text, Is.EqualTo("e"));
            Assert.That(results[1].Index, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() =>
                GlyphPlacer.Place(slots, new[] { "M" }, Metrics, new PlacementOptions(100), results));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlacementOptions(0));
        }
    

        [Test]
        public void EdgeAnchorsHangFromATopEdgeAndRestOnABottomEdge()
        {
            var top = GlyphPlacer.Place(Right, "e", Metrics, new PlacementOptions(100, vertical: VerticalAnchor.CapTop));
            Assert.That(top.Origin.Y, Is.EqualTo(370).Within(1e-9));

            var bottom = GlyphPlacer.Place(Right, "g", Metrics,
                new PlacementOptions(100, vertical: VerticalAnchor.EmBottom));
            Assert.That(bottom.Origin.Y, Is.EqualTo(280).Within(1e-9));
        }
    }
}
