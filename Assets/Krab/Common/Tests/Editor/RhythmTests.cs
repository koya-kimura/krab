using Krab.Common.Utils;
using NUnit.Framework;

namespace Krab.Common.Tests.Editor
{
    public sealed class RhythmTests
    {
        private const float Tolerance = 1e-5f;

        private static readonly EasingFn Linear = x => x;

        [TestCase(0f, 0f)]
        [TestCase(4f, 1f)]
        [TestCase(5f, 1.25f)]
        [TestCase(-1f, -0.25f)]
        public void GetPhase_EaseAsLongAsCycle_CountsContinuously(float t, float expected)
        {
            Assert.That(Rhythm.GetPhase(t, 4f, 4f, Linear), Is.EqualTo(expected).Within(Tolerance));
        }

        [TestCase(0f, 0f)]
        [TestCase(2.9f, 0f)]
        [TestCase(3.5f, 0.5f)]
        [TestCase(4f, 1f)]
        [TestCase(6f, 1f)]
        public void GetPhase_ShortEase_HoldsThenMovesAtCycleEnd(float t, float expected)
        {
            Assert.That(Rhythm.GetPhase(t, 4f, 1f, Linear), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void GetPhase_DefaultEasing_StaysWithinCycle()
        {
            var phase = Rhythm.GetPhase(9.5f, 4f, 2f);

            Assert.That(phase, Is.GreaterThanOrEqualTo(2f).And.LessThanOrEqualTo(3f));
        }

        [Test]
        public void GetPhase_Offset_ShiftsByOffsetOverCycle()
        {
            const float cycle = 8f;
            var a = Rhythm.GetPhase(10f, cycle, cycle, Linear);
            var b = Rhythm.GetPhase(10f + 2f, cycle, cycle, Linear);

            Assert.That(b - a, Is.EqualTo(2f / cycle).Within(Tolerance));
        }

        [Test]
        public void GetPhase_ZeroEase_JumpsAtBoundaryWithoutNaN()
        {
            Assert.That(Rhythm.GetPhase(3f, 4f, 0f, Linear), Is.EqualTo(0f));
            Assert.That(Rhythm.GetPhase(4f, 4f, 0f, Linear), Is.EqualTo(1f));
        }

        [Test]
        public void GetPhase_ZeroCycle_StaysFinite()
        {
            Assert.That(float.IsNaN(Rhythm.GetPhase(1f, 0f, 0f, Linear)), Is.False);
            Assert.That(float.IsInfinity(Rhythm.GetPhase(1f, 0f, 0f, Linear)), Is.False);
        }

        [TestCase(0f, 0.25f, 0)]
        [TestCase(0.99f, 0.25f, 3)]
        [TestCase(1f, 0.25f, 4)]
        [TestCase(7f, 2f, 3)]
        [TestCase(-3f, 1f, 0)]
        public void GetIndex_AllowsFractionalIntervals(float t, float interval, int expected)
        {
            Assert.That(Rhythm.GetIndex(t, interval), Is.EqualTo(expected));
        }

        [Test]
        public void GetIndex_ZeroInterval_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Rhythm.GetIndex(1f, 0f));
        }

        [Test]
        public void GetLerpNoise_ZeroEase_StaysInRange()
        {
            var value = Rhythm.GetLerpNoise(5f, 2f, 0f, 7u, Linear);

            Assert.That(value, Is.InRange(0f, 1f));
        }
    }
}
