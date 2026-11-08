using System;
using Krab.ShapeSynth.Core;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests
{
    public class AdsrTests
    {
        private static AdsrInstance Create(bool autoRelease = true) =>
            new AdsrInstance(new AdsrDefinition(2, 2, 0.5, 2, 2), new VoiceTime(10, 60), autoRelease);

        [TestCase(9, 0)]
        [TestCase(10, 0)]
        [TestCase(11, 0.5)]
        [TestCase(12, 1)]
        [TestCase(13, 0.75)]
        [TestCase(14, 0.5)]
        [TestCase(16, 0.5)]
        [TestCase(17, 0.25)]
        [TestCase(18, 0)]
        [TestCase(100000, 0)]
        public void BoundariesAndLargeJump(double seconds, double expected)
        {
            Assert.That(Create().Evaluate(seconds), Is.EqualTo(expected).Within(1e-12));
        }

        [TestCase(11, 0.5)]
        [TestCase(13, 0.75)]
        [TestCase(15, 0.5)]
        public void ExternalReleaseStartsFromCurrentValue(double seconds, double value)
        {
            var instance = Create();
            instance.Release(seconds);
            Assert.That(instance.Evaluate(seconds), Is.EqualTo(value).Within(1e-12));
            Assert.That(instance.Evaluate(seconds + 1), Is.EqualTo(value / 2).Within(1e-12));
            Assert.That(instance.Evaluate(seconds + 0.5), Is.EqualTo(value * 0.75).Within(1e-12));
            Assert.That(instance.IsFinished(seconds + 1.99), Is.False);
            Assert.That(instance.IsFinished(seconds + 2), Is.True);
            Assert.That(instance.Evaluate(seconds + 2), Is.Zero);
        }

        [Test]
        public void ZeroSustainTimeReleasesAtDecayEnd()
        {
            var instance = new AdsrInstance(new AdsrDefinition(1, 1, 0.5, 0, 2), new VoiceTime(0, 60));
            Assert.That(instance.Evaluate(2), Is.EqualTo(0.5));
            Assert.That(instance.Evaluate(3), Is.EqualTo(0.25));
            Assert.That(instance.IsFinished(4), Is.True);
        }

        [TestCase(0, 1, 1, 1, 0, 1)]
        [TestCase(1, 0, 1, 1, 1, 0.5)]
        [TestCase(1, 1, 0, 1, 2.5, 0.25)]
        [TestCase(1, 1, 1, 0, 3, 0)]
        [TestCase(0, 0, 1, 1, 0, 0.5)]
        [TestCase(0, 0, 0, 0, 0, 0)]
        public void ZeroDurationsSkipSegments(double a, double d, double s, double r, double beat, double expected)
        {
            var instance = new AdsrInstance(new AdsrDefinition(a, d, 0.5, s, r), new VoiceTime(0, 60));
            Assert.That(instance.Evaluate(beat), Is.EqualTo(expected).Within(1e-12));
            Assert.That(instance.IsFinished(a + d + s + r), Is.True);
        }

        [Test]
        public void HeldSustainWaitsForExternalRelease()
        {
            var instance = Create(false);
            Assert.That(instance.Evaluate(1000), Is.EqualTo(0.5));
            Assert.That(instance.IsFinished(1000), Is.False);
            instance.Release(1000);
            instance.Release(1001);
            Assert.That(instance.Evaluate(1001), Is.EqualTo(0.25));
            Assert.That(instance.IsFinished(1002), Is.True);
        }

        [Test]
        public void LateReleaseDoesNotRestartAutomaticRelease()
        {
            var instance = Create();
            instance.Release(17);
            Assert.That(instance.Evaluate(17), Is.EqualTo(0.25));
            Assert.That(instance.IsFinished(18), Is.True);
        }

        [Test]
        public void InstancesShareOnlyImmutableDefinitionAndSnapshotTempo()
        {
            var definition = new AdsrDefinition(2, 2, 0.5, 2, 2);
            double globalBpm = 60;
            var first = new AdsrInstance(definition, new VoiceTime(10, globalBpm));
            globalBpm = 120;
            var second = new AdsrInstance(definition, new VoiceTime(10, globalBpm));
            Assert.That(first.Evaluate(11), Is.EqualTo(0.5));
            Assert.That(second.Evaluate(11), Is.EqualTo(1));
            first.Release(11);
            Assert.That(first.Evaluate(12), Is.EqualTo(0.25));
            Assert.That(second.Evaluate(12), Is.EqualTo(0.5));
            Assert.That(first.Time.StartBpm, Is.EqualTo(60));
        }

        [Test]
        public void TimeSnapshotValidatesAndUsesAbsoluteSeconds()
        {
            var time = new VoiceTime(100, 120);
            Assert.That(time.ElapsedBeats(101), Is.EqualTo(2));
            Assert.That(time.ElapsedBeats(99), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceTime(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceTime(double.NaN, 60));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.ElapsedBeats(double.PositiveInfinity));
            Assert.Throws<InvalidOperationException>(() => new AdsrInstance(new AdsrDefinition(0, 0, 0, 0, 0), default));
        }

        [Test]
        public void InvalidDefinitionsAndReleaseAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(-1, 0, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, -1, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, 0, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, 0, 0, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, 0, 1.1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, 0, -0.1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(double.NaN, 0, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(0, 0, 0, 0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(double.MaxValue, double.MaxValue, 0, 0, 0));
            Assert.Throws<ArgumentNullException>(() => new AdsrInstance(null, new VoiceTime(0, 60)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().Release(9));
        }
    }
}
