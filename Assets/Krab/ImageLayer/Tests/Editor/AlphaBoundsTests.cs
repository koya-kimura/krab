using System;
using Krab.ImageLayer.Core.Geometry;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class AlphaBoundsTests
    {
        [Test]
        public void FindsTheOpaqueBoxInTopDownRows()
        {
            var pixels = Image(6, 4);
            SetAlpha(pixels, 6, 1, 2, 255);
            SetAlpha(pixels, 6, 3, 1, 10);

            Assert.That(AlphaBounds.TryFind(pixels, 6, 4, false, out var bounds), Is.True);
            Assert.That(bounds, Is.EqualTo(PixelRect.FromEdges(1, 1, 4, 3)));
        }

        [Test]
        public void FlipsBottomUpRowsIntoTopLeftCoordinates()
        {
            var pixels = Image(4, 5);
            // Storage row 0 is the bottom row in Unity's order.
            SetAlpha(pixels, 4, 2, 0, 255);
            SetAlpha(pixels, 4, 1, 1, 255);

            Assert.That(AlphaBounds.TryFind(pixels, 4, 5, true, out var bounds), Is.True);
            Assert.That(bounds, Is.EqualTo(PixelRect.FromEdges(1, 3, 3, 5)));
        }

        [Test]
        public void ThresholdTreatsFaintPixelsAsTransparent()
        {
            var pixels = Image(4, 4);
            SetAlpha(pixels, 4, 0, 0, 3);
            SetAlpha(pixels, 4, 2, 2, 200);

            AlphaBounds.TryFind(pixels, 4, 4, false, out var all);
            AlphaBounds.TryFind(pixels, 4, 4, false, out var strong, threshold: 3);
            Assert.That(all, Is.EqualTo(PixelRect.FromEdges(0, 0, 3, 3)));
            Assert.That(strong, Is.EqualTo(new PixelRect(2, 2, 1, 1)));
        }

        [Test]
        public void FullyTransparentAndFullyOpaqueImages()
        {
            var empty = Image(3, 2);
            Assert.That(AlphaBounds.TryFind(empty, 3, 2, false, out _), Is.False);

            var full = Image(3, 2);
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                SetAlpha(full, 3, x, y, 255);

            AlphaBounds.TryFind(full, 3, 2, true, out var bounds);
            Assert.That(bounds, Is.EqualTo(new PixelRect(0, 0, 3, 2)));
        }

        [Test]
        public void RejectsDataThatDoesNotMatchTheSize()
        {
            Assert.Throws<ArgumentException>(() => AlphaBounds.TryFind(new byte[15], 2, 2, false, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => AlphaBounds.TryFind(new byte[0], 0, 2, false, out _));
        }

        [Test]
        public void UnionGrowsToContainBothAndIgnoresEmpty()
        {
            var a = new PixelRect(10, 20, 5, 5);
            var b = new PixelRect(0, 22, 4, 10);
            Assert.That(a.Union(b), Is.EqualTo(PixelRect.FromEdges(0, 20, 15, 32)));
            Assert.That(default(PixelRect).Union(a), Is.EqualTo(a));
            Assert.That(a.Union(default), Is.EqualTo(a));
        }

        [Test]
        public void TrimmedFrameRejectsContentOutsideTheSource()
        {
            Assert.DoesNotThrow(() => new TrimmedFrame(100, 50, new PixelRect(90, 40, 10, 10)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TrimmedFrame(100, 50, new PixelRect(91, 40, 10, 10)));
            Assert.That(new TrimmedFrame(100, 50, default).IsEmpty, Is.True);
        }

        private static byte[] Image(int width, int height) => new byte[width * height * 4];

        private static void SetAlpha(byte[] pixels, int width, int x, int row, byte alpha)
        {
            pixels[(row * width + x) * 4 + 3] = alpha;
        }
    }
}
