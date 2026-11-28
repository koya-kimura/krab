using System;
using System.Linq;
using Krab.ImageLayer.Core.Frames;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class FrameSelectorTests
    {
        [Test]
        public void ClampHoldsTheEndFrames()
        {
            Assert.That(Positions(-2, 6, 4, FrameWrap.Clamp), Is.EqualTo(new[] { 0, 0, 0, 1, 2, 3, 3, 3, 3 }));
            Assert.That(FrameSelector.FromPosition(2.99, 4, FrameWrap.Clamp), Is.EqualTo(2));
        }

        [Test]
        public void LoopWrapsInBothDirections()
        {
            Assert.That(Positions(-3, 6, 3, FrameWrap.Loop), Is.EqualTo(new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2, 0 }));
            Assert.That(FrameSelector.FromPosition(-0.5, 3, FrameWrap.Loop), Is.EqualTo(2));
        }

        [Test]
        public void PingPongTurnsAroundWithoutRepeatingTheEnds()
        {
            Assert.That(Positions(0, 8, 4, FrameWrap.PingPong), Is.EqualTo(new[] { 0, 1, 2, 3, 2, 1, 0, 1, 2 }));
            Assert.That(Positions(-3, -1, 4, FrameWrap.PingPong), Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(Positions(0, 3, 2, FrameWrap.PingPong), Is.EqualTo(new[] { 0, 1, 0, 1 }));
        }

        [Test]
        public void ASingleFrameClipAlwaysShowsItsFrame()
        {
            foreach (FrameWrap wrap in Enum.GetValues(typeof(FrameWrap)))
                Assert.That(Positions(-5, 5, 1, wrap).Distinct(), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void ProgressOfOneIsOnePassThroughTheClip()
        {
            Assert.That(FrameSelector.FromProgress(0, 40, FrameWrap.Clamp), Is.EqualTo(0));
            Assert.That(FrameSelector.FromProgress(0.999, 40, FrameWrap.Clamp), Is.EqualTo(39));
            Assert.That(FrameSelector.FromProgress(1, 40, FrameWrap.Clamp), Is.EqualTo(39));
            Assert.That(FrameSelector.FromProgress(1, 40, FrameWrap.Loop), Is.EqualTo(0));
            Assert.That(FrameSelector.FromProgress(1.5, 40, FrameWrap.Loop), Is.EqualTo(20));
            Assert.That(FrameSelector.FromProgress(1, 5, FrameWrap.PingPong), Is.EqualTo(4));
            Assert.That(FrameSelector.FromProgress(1.5, 5, FrameWrap.PingPong), Is.EqualTo(2));
            Assert.That(FrameSelector.FromProgress(2, 5, FrameWrap.PingPong), Is.EqualTo(0));
        }

        [Test]
        public void ProgressSpendsEqualTimeOnEachFrame()
        {
            var counts = new int[8];
            for (int step = 0; step < 800; step++)
                counts[FrameSelector.FromProgress(step / 800.0, 8, FrameWrap.Loop)]++;

            Assert.That(counts.Distinct(), Is.EqualTo(new[] { 100 }));
        }

        [Test]
        public void RejectsNonFinitePositionsAndEmptyClips()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FrameSelector.FromPosition(double.NaN, 3, FrameWrap.Loop));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FrameSelector.FromProgress(double.PositiveInfinity, 3, FrameWrap.Clamp));
            Assert.Throws<ArgumentOutOfRangeException>(() => FrameSelector.FromPosition(0, 0, FrameWrap.Loop));
        }

        private static int[] Positions(int from, int to, int frameCount, FrameWrap wrap)
        {
            return Enumerable.Range(from, to - from + 1)
                .Select(position => FrameSelector.FromPosition(position, frameCount, wrap))
                .ToArray();
        }
    }
}
