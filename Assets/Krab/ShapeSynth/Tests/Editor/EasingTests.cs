using System;
using System.Linq;
using Krab.ShapeSynth.Core.Definitions;
using Krab.ShapeSynth.Core.Modulation;
using NUnit.Framework;

namespace Krab.ShapeSynth.Tests.Editor
{
    public sealed class EasingTests
    {
        private static readonly EasingCurve[] AllCurves = (EasingCurve[])Enum.GetValues(typeof(EasingCurve));

        private static readonly (EasingCurve In, EasingCurve Out, EasingCurve InOut)[] Families =
        {
            (EasingCurve.InSine, EasingCurve.OutSine, EasingCurve.InOutSine),
            (EasingCurve.InQuad, EasingCurve.OutQuad, EasingCurve.InOutQuad),
            (EasingCurve.InCubic, EasingCurve.OutCubic, EasingCurve.InOutCubic),
            (EasingCurve.InExpo, EasingCurve.OutExpo, EasingCurve.InOutExpo),
            (EasingCurve.InBack, EasingCurve.OutBack, EasingCurve.InOutBack),
            (EasingCurve.InElastic, EasingCurve.OutElastic, EasingCurve.InOutElastic),
            (EasingCurve.InBounce, EasingCurve.OutBounce, EasingCurve.InOutBounce)
        };

        [Test]
        public void EveryCurveStartsAtZeroEndsAtOneAndClampsOutside()
        {
            foreach (var curve in AllCurves)
            {
                Assert.That(EasingFunctions.Evaluate(curve, -1), Is.EqualTo(0), curve.ToString());
                Assert.That(EasingFunctions.Evaluate(curve, 0), Is.EqualTo(0), curve.ToString());
                Assert.That(EasingFunctions.Evaluate(curve, 1), Is.EqualTo(1), curve.ToString());
                Assert.That(EasingFunctions.Evaluate(curve, 2), Is.EqualTo(1), curve.ToString());
            }
        }

        [Test]
        public void EveryCurveIsContinuousNearTheEndsAndTheMidpoint()
        {
            const double step = 1e-7;
            foreach (var curve in AllCurves)
            {
                Assert.That(EasingFunctions.Evaluate(curve, step), Is.EqualTo(0).Within(1e-3), curve.ToString());
                Assert.That(EasingFunctions.Evaluate(curve, 1 - step), Is.EqualTo(1).Within(1e-3), curve.ToString());
                Assert.That(EasingFunctions.Evaluate(curve, 0.5 - step),
                    Is.EqualTo(EasingFunctions.Evaluate(curve, 0.5)).Within(1e-4), curve.ToString());
            }
        }

        [TestCase(EasingCurve.Linear, 0.25, 0.25)]
        [TestCase(EasingCurve.InQuad, 0.5, 0.25)]
        [TestCase(EasingCurve.InCubic, 0.5, 0.125)]
        [TestCase(EasingCurve.OutBounce, 1 / 2.75, 1)]
        public void KnownValues(EasingCurve curve, double progress, double expected)
        {
            Assert.That(EasingFunctions.Evaluate(curve, progress), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void OutMirrorsInAndInOutPassesThroughTheMiddle()
        {
            foreach (var (inCurve, outCurve, inOutCurve) in Families)
            {
                foreach (var x in new[] { 0.1, 0.3, 0.6, 0.9 })
                {
                    Assert.That(EasingFunctions.Evaluate(outCurve, x),
                        Is.EqualTo(1 - EasingFunctions.Evaluate(inCurve, 1 - x)).Within(1e-9), outCurve.ToString());
                }

                Assert.That(EasingFunctions.Evaluate(inOutCurve, 0.5), Is.EqualTo(0.5).Within(1e-9),
                    inOutCurve.ToString());
            }
        }

        [Test]
        public void BackAndElasticOvershootWhileOthersStayInRange()
        {
            var samples = Enumerable.Range(1, 999).Select(i => i / 1000.0).ToArray();

            Assert.That(samples.Max(x => EasingFunctions.Evaluate(EasingCurve.OutBack, x)), Is.GreaterThan(1));
            Assert.That(samples.Min(x => EasingFunctions.Evaluate(EasingCurve.InBack, x)), Is.LessThan(0));
            Assert.That(samples.Max(x => EasingFunctions.Evaluate(EasingCurve.OutElastic, x)), Is.GreaterThan(1));
            foreach (var curve in new[] { EasingCurve.InOutCubic, EasingCurve.OutExpo, EasingCurve.InOutBounce })
            {
                Assert.That(samples.All(x => EasingFunctions.Evaluate(curve, x) is >= 0 and <= 1), curve.ToString());
            }
        }

        [Test]
        public void RejectsNaNProgress()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EasingFunctions.Evaluate(EasingCurve.Linear, double.NaN));
        }
    }
}
