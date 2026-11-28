using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Voices;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class TimerTests
    {
        [Test]
        public void ZeroDurationFinishesAtStartButNotBefore()
        {
            var timer = new TimerInstance(new TimerDefinition(0), new VoiceTime(10, 60));

            Assert.That(timer.IsFinished(9), Is.False);
            Assert.That(timer.IsFinished(10), Is.True);
        }

        [Test]
        public void DurationBoundaryIsInclusiveAndUsesCapturedBpm()
        {
            double bpm = 60;
            var timer = new TimerInstance(new TimerDefinition(2), new VoiceTime(10, bpm));
            bpm = 120;

            Assert.That(timer.IsFinished(11.999), Is.False);
            Assert.That(timer.IsFinished(12), Is.True);
            Assert.That(timer.Time.StartBpm, Is.EqualTo(60));
        }

        [Test]
        public void HugeJumpFinishesAndRepeatedQueriesAreStable()
        {
            var timer = new TimerInstance(new TimerDefinition(1), new VoiceTime(0, 60));

            Assert.That(timer.IsFinished(1e300), Is.True);
            Assert.That(timer.IsFinished(0.5), Is.False);
            Assert.That(timer.IsFinished(1e300), Is.True);
        }

        [Test]
        public void InstancesKeepIndependentDefinitionsAndSnapshots()
        {
            var definition = new TimerDefinition(2);
            var slow = new TimerInstance(definition, new VoiceTime(0, 60));
            var fast = new TimerInstance(definition, new VoiceTime(0, 120));

            Assert.That(slow.IsFinished(1), Is.False);
            Assert.That(fast.IsFinished(1), Is.True);
            Assert.That(slow.IsFinished(2), Is.True);
        }

        [Test]
        public void InvalidDurationAndInstanceArgumentsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimerDefinition(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimerDefinition(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimerDefinition(double.PositiveInfinity));
            Assert.Throws<ArgumentNullException>(() => new TimerInstance(null, new VoiceTime(0, 60)));
            Assert.Throws<InvalidOperationException>(() => new TimerInstance(new TimerDefinition(1), default));

            var timer = new TimerInstance(new TimerDefinition(1), new VoiceTime(0, 60));
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.IsFinished(double.PositiveInfinity));
        }
    }
}
