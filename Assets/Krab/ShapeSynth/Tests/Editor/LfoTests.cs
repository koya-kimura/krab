using System;
using Krab.ShapeSynth.Core;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests
{
    public class LfoTests
    {
        private static LfoInstance Create(LfoWaveform waveform, double cyclesPerBeat = 1, double phaseOffset = 0,
            double startSeconds = 0, double startBpm = 60) =>
            new LfoInstance(new LfoDefinition(cyclesPerBeat, phaseOffset, waveform), new VoiceTime(startSeconds, startBpm));

        [TestCase(LfoWaveform.Sine, 0, 0)]
        [TestCase(LfoWaveform.Sine, 0.25, 1)]
        [TestCase(LfoWaveform.Sine, 0.5, 0)]
        [TestCase(LfoWaveform.Sine, 0.75, -1)]
        [TestCase(LfoWaveform.Sine, 1, 0)]
        [TestCase(LfoWaveform.Triangle, 0, 0)]
        [TestCase(LfoWaveform.Triangle, 0.25, 1)]
        [TestCase(LfoWaveform.Triangle, 0.5, 0)]
        [TestCase(LfoWaveform.Triangle, 0.75, -1)]
        [TestCase(LfoWaveform.Triangle, 1, 0)]
        [TestCase(LfoWaveform.Sawtooth, 0, -1)]
        [TestCase(LfoWaveform.Sawtooth, 0.25, -0.5)]
        [TestCase(LfoWaveform.Sawtooth, 0.5, 0)]
        [TestCase(LfoWaveform.Sawtooth, 0.75, 0.5)]
        [TestCase(LfoWaveform.Sawtooth, 1, -1)]
        [TestCase(LfoWaveform.Square, 0, 1)]
        [TestCase(LfoWaveform.Square, 0.25, 1)]
        [TestCase(LfoWaveform.Square, 0.5, -1)]
        [TestCase(LfoWaveform.Square, 0.75, -1)]
        [TestCase(LfoWaveform.Square, 1, 1)]
        public void WaveformsHaveExpectedPhaseBoundaries(LfoWaveform waveform, double phase, double expected)
        {
            Assert.That(Create(waveform).Evaluate(phase), Is.EqualTo(expected).Within(1e-12));
        }

        [TestCase(0.25, 0.3826834323650898)]
        [TestCase(0.5, 0.7071067811865475)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        public void FrequencyIsCyclesPerBeat(double cyclesPerBeat, double valueAtQuarterSecond)
        {
            Assert.That(Create(LfoWaveform.Sine, cyclesPerBeat).Evaluate(0.25),
                Is.EqualTo(valueAtQuarterSecond).Within(1e-12));
        }

        [TestCase(0.25, 1)]
        [TestCase(-0.25, -1)]
        [TestCase(1.25, 1)]
        public void SineOffsetChangesTheStartingSignal(double offset, double expected)
        {
            var instance = Create(LfoWaveform.Sine, phaseOffset: offset, startSeconds: 10);
            Assert.That(instance.Evaluate(10), Is.EqualTo(expected).Within(1e-12));
            Assert.That(instance.Evaluate(9), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void OffsetsWrapInBothDirectionsAndApplyBeforeTrigger()
        {
            var positive = Create(LfoWaveform.Square, phaseOffset: 1.25, startSeconds: 10);
            var negative = Create(LfoWaveform.Square, phaseOffset: -0.75, startSeconds: 10);
            Assert.That(positive.Definition.PhaseOffset, Is.EqualTo(0.25));
            Assert.That(negative.Definition.PhaseOffset, Is.EqualTo(0.25));
            Assert.That(positive.Evaluate(5), Is.EqualTo(1));
            Assert.That(negative.Evaluate(10), Is.EqualTo(1));
        }

        [Test]
        public void PhaseRepeatsAndEvaluationOrderDoesNotChangeState()
        {
            var instance = Create(LfoWaveform.Triangle, 0.25, 0.1);
            var atFour = instance.Evaluate(4);
            var atHalf = instance.Evaluate(0.5);
            Assert.That(instance.Evaluate(0.5), Is.EqualTo(atHalf).Within(1e-12));
            Assert.That(instance.Evaluate(4), Is.EqualTo(atFour).Within(1e-12));
            Assert.That(instance.Evaluate(1000004), Is.EqualTo(atFour).Within(1e-9));
        }

        [Test]
        public void InstancesSnapshotTempoAndKeepIndependentStartTimes()
        {
            var definition = new LfoDefinition(0.25, 0, LfoWaveform.Sine);
            double bpm = 60;
            var first = new LfoInstance(definition, new VoiceTime(0, bpm));
            bpm = 120;
            var second = new LfoInstance(definition, new VoiceTime(2, bpm));

            Assert.That(first.Evaluate(1), Is.EqualTo(Math.Sin(Math.PI / 2)).Within(1e-12));
            Assert.That(second.Evaluate(3), Is.EqualTo(Math.Sin(Math.PI)).Within(1e-12));
            Assert.That(first.Time.StartBpm, Is.EqualTo(60));
            Assert.That(second.Time.StartBpm, Is.EqualTo(120));
            Assert.That(first.Evaluate(2), Is.EqualTo(0).Within(1e-12));
        }

        [Test]
        public void LargeFiniteTimePreservesRepresentableFractionalPhase()
        {
            var instance = Create(LfoWaveform.Sine, 1, startBpm: 60);
            Assert.That(instance.Evaluate(1e12 + 0.25), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void AllWaveformsStayWithinUnitRangeAcrossLargeJumps()
        {
            foreach (LfoWaveform waveform in Enum.GetValues(typeof(LfoWaveform)))
            {
                var instance = Create(waveform, 3.7, -1.25);
                foreach (var seconds in new[] { 0.0, 0.123, 1.0, 1000000.25, 1e12 + 0.25 })
                {
                    var value = instance.Evaluate(seconds);
                    Assert.That(value, Is.InRange(-1, 1), waveform + " at " + seconds);
                }
            }
        }

        [Test]
        public void ZeroFrequencyFreezesAtOffset()
        {
            foreach (LfoWaveform waveform in Enum.GetValues(typeof(LfoWaveform)))
            {
                var instance = Create(waveform, 0, 0.25);
                var initial = instance.Evaluate(0);
                Assert.That(instance.Evaluate(-100), Is.EqualTo(initial).Within(1e-12));
                Assert.That(instance.Evaluate(1e12), Is.EqualTo(initial).Within(1e-12));
            }
        }

        [Test]
        public void ExtremeFiniteFrequenciesDoNotOverflowTheCycleProduct()
        {
            var fast = Create(LfoWaveform.Sine, double.MaxValue);
            Assert.That(fast.Evaluate(double.MaxValue), Is.InRange(-1, 1));
            var slow = Create(LfoWaveform.Sine, double.Epsilon, 0.25);
            Assert.That(slow.Evaluate(1), Is.EqualTo(1).Within(1e-12));
        }

        [Test]
        public void InvalidInputsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(1, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(1, double.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LfoDefinition(1, 0, (LfoWaveform)999));
            Assert.Throws<ArgumentNullException>(() => new LfoInstance(null, new VoiceTime(0, 60)));
            Assert.Throws<InvalidOperationException>(() => new LfoInstance(new LfoDefinition(1), default));

            var instance = Create(LfoWaveform.Sine);
            Assert.Throws<ArgumentOutOfRangeException>(() => instance.Evaluate(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => instance.Evaluate(double.PositiveInfinity));
            var extremeStart = Create(LfoWaveform.Sine, startSeconds: -double.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => extremeStart.Evaluate(double.MaxValue));
        }
    }
}
