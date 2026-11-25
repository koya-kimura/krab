using System;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using Krab.ShapeSynth.Core.Presets;
using Krab.ShapeSynth.Core.Runtime;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class AdsrCurveTests
    {
        // A=2, D=2, S=0.5 for 2, R=2 beats at 60 BPM from t=10: attack [10,12), decay [12,14), release [16,18).
        [TestCase(10, 0)]
        [TestCase(11, 0.25)]
        [TestCase(12, 1)]
        [TestCase(13, 0.625)]
        [TestCase(14, 0.5)]
        [TestCase(16, 0.5)]
        [TestCase(17, 0.375)]
        [TestCase(18, 0)]
        public void EachSegmentFollowsItsOwnCurve(double seconds, double expected)
        {
            Assert.That(Create().Evaluate(seconds), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void SegmentsJoinWithoutJumps()
        {
            var envelope = Create();
            const double step = 1e-7;
            foreach (var boundary in new[] { 12.0, 14.0, 16.0 })
            {
                Assert.That(envelope.Evaluate(boundary - step), Is.EqualTo(envelope.Evaluate(boundary)).Within(1e-5),
                    boundary.ToString());
            }
        }

        [Test]
        public void ExternalReleaseCurvesDownFromTheCurrentValue()
        {
            var envelope = Create(false);
            envelope.Release(11);

            Assert.That(envelope.Evaluate(11), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(envelope.Evaluate(12), Is.EqualTo(0.25 * 0.75).Within(1e-12));
            Assert.That(envelope.Evaluate(13), Is.EqualTo(0));
        }

        [Test]
        public void RejectsUnknownCurves()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdsrDefinition(1, 1, 1, 1, 1, (EasingCurve)999));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AdsrDefinition(1, 1, 1, 1, 1, decayCurve: (EasingCurve)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AdsrDefinition(1, 1, 1, 1, 1, releaseCurve: (EasingCurve)999));
        }

        [Test]
        public void PresetCurvesReachTheCompiledEnvelope()
        {
            var component = PresetComponent.Create(ModulatorKind.Adsr);
            component.AttackCurve = EasingCurve.OutCubic;
            component.DecayCurve = EasingCurve.InSine;
            component.ReleaseCurve = EasingCurve.OutBounce;

            var definition = (AdsrDefinition)PresetCompiler.ModulatorDefinition(component);

            Assert.That(definition.AttackCurve, Is.EqualTo(EasingCurve.OutCubic));
            Assert.That(definition.DecayCurve, Is.EqualTo(EasingCurve.InSine));
            Assert.That(definition.ReleaseCurve, Is.EqualTo(EasingCurve.OutBounce));
        }

        private static AdsrInstance Create(bool autoRelease = true)
        {
            var definition = new AdsrDefinition(2, 2, 0.5, 2, 2,
                EasingCurve.InQuad, EasingCurve.OutQuad, EasingCurve.InQuad);
            return new AdsrInstance(definition, new VoiceTime(10, 60), autoRelease);
        }
    }
}
