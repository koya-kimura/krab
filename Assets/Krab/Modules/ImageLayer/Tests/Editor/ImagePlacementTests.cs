using System;
using Krab.ImageLayer.Core.Geometry;
using Krab.ImageLayer.Core.Placement;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class ImagePlacementTests
    {
        [Test]
        public void ImageModesReadTheFourNumbersLikeP5()
        {
            Assert.That(ImagePlacement.Resolve(ImageMode.Corner, 10, 20, 100, 50),
                Is.EqualTo(new DrawRect(10, 20, 100, 50)));
            Assert.That(ImagePlacement.Resolve(ImageMode.Center, 10, 20, 100, 50),
                Is.EqualTo(new DrawRect(-40, -5, 100, 50)));
            Assert.That(ImagePlacement.Resolve(ImageMode.Corners, 110, 70, 10, 20),
                Is.EqualTo(new DrawRect(10, 20, 100, 50)));
        }

        [Test]
        public void ContentQuadSitsWhereItWasInTheUntrimmedCanvas()
        {
            // A walk frame: 1280x720 canvas with the figure in the middle, feet on the bottom edge.
            var frame = new TrimmedFrame(1280, 720, PixelRect.FromEdges(477, 63, 813, 720));

            Assert.That(ImagePlacement.MapContent(new DrawRect(0, 0, 1280, 720), frame),
                Is.EqualTo(DrawRect.FromEdges(477, 63, 813, 720)));

            // Drawn at half size from (100, 50): the content moves and scales with the canvas.
            Assert.That(ImagePlacement.MapContent(new DrawRect(100, 50, 640, 360), frame),
                Is.EqualTo(DrawRect.FromEdges(338.5, 81.5, 506.5, 410)));
        }

        [Test]
        public void ContainFitsTheWholeCanvasAndCenters()
        {
            var box = new DrawRect(0, 0, 1920, 1080);
            var canvas = new PixelRect(0, 0, 1000, 1000);

            Assert.That(ImagePlacement.Fit(box, 1000, 1000, canvas, FitMode.Contain),
                Is.EqualTo(new DrawRect(420, 0, 1080, 1080)));
            Assert.That(ImagePlacement.Fit(box, 1000, 1000, canvas, FitMode.Cover),
                Is.EqualTo(new DrawRect(0, -420, 1920, 1920)));
            Assert.That(ImagePlacement.Fit(box, 1000, 1000, canvas, FitMode.Contain, 0, 1),
                Is.EqualTo(new DrawRect(0, 0, 1080, 1080)));
        }

        [Test]
        public void FittingTheContentScalesTheCanvasSoTheFigureFillsTheBox()
        {
            // Figure is 200x400 at (100, 50) in a 1000x500 canvas; fit it into a 100x100 cell.
            var content = new PixelRect(100, 50, 200, 400);
            var canvasRect = ImagePlacement.Fit(new DrawRect(0, 0, 100, 100), 1000, 500, content, FitMode.Contain);

            Assert.That(canvasRect, Is.EqualTo(new DrawRect(0, -12.5, 250, 125)));
            var frame = new TrimmedFrame(1000, 500, content);
            Assert.That(ImagePlacement.MapContent(canvasRect, frame), Is.EqualTo(new DrawRect(25, 0, 50, 100)));
        }

        [Test]
        public void FitRejectsAnEmptyReference()
        {
            Assert.Throws<ArgumentException>(() =>
                ImagePlacement.Fit(new DrawRect(0, 0, 10, 10), 10, 10, default, FitMode.Contain));
        }
    }
}
