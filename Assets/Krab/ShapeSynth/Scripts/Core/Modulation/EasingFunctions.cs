using System;
using Krab.ShapeSynth.Core.Definitions;

namespace Krab.ShapeSynth.Core.Modulation
{
    /// <summary>
    /// Maps progress 0..1 to a curve value. The ends are exactly 0 and 1; Back and Elastic overshoot in between.
    /// </summary>
    public static class EasingFunctions
    {
        private const double BackOvershoot = 1.70158;
        private const double BackInOutOvershoot = BackOvershoot * 1.525;
        private const double ElasticPeriod = 2 * Math.PI / 3;
        private const double ElasticInOutPeriod = 2 * Math.PI / 4.5;

        public static double Evaluate(EasingCurve curve, double progress)
        {
            if (double.IsNaN(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
            if (progress <= 0) return 0;
            if (progress >= 1) return 1;

            var x = progress;
            switch (curve)
            {
                case EasingCurve.Linear:
                    return x;
                case EasingCurve.InSine:
                    return 1 - Math.Cos(x * Math.PI / 2);
                case EasingCurve.OutSine:
                    return Math.Sin(x * Math.PI / 2);
                case EasingCurve.InOutSine:
                    return -(Math.Cos(Math.PI * x) - 1) / 2;
                case EasingCurve.InQuad:
                    return x * x;
                case EasingCurve.OutQuad:
                    return 1 - (1 - x) * (1 - x);
                case EasingCurve.InOutQuad:
                    return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
                case EasingCurve.InCubic:
                    return x * x * x;
                case EasingCurve.OutCubic:
                    return 1 - Math.Pow(1 - x, 3);
                case EasingCurve.InOutCubic:
                    return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
                case EasingCurve.InExpo:
                    return Math.Pow(2, 10 * x - 10);
                case EasingCurve.OutExpo:
                    return 1 - Math.Pow(2, -10 * x);
                case EasingCurve.InOutExpo:
                    return x < 0.5 ? Math.Pow(2, 20 * x - 10) / 2 : (2 - Math.Pow(2, -20 * x + 10)) / 2;
                case EasingCurve.InBack:
                    return (BackOvershoot + 1) * x * x * x - BackOvershoot * x * x;
                case EasingCurve.OutBack:
                    return 1 + (BackOvershoot + 1) * Math.Pow(x - 1, 3) + BackOvershoot * Math.Pow(x - 1, 2);
                case EasingCurve.InOutBack:
                    return InOutBack(x);
                case EasingCurve.InElastic:
                    return -Math.Pow(2, 10 * x - 10) * Math.Sin((10 * x - 10.75) * ElasticPeriod);
                case EasingCurve.OutElastic:
                    return Math.Pow(2, -10 * x) * Math.Sin((10 * x - 0.75) * ElasticPeriod) + 1;
                case EasingCurve.InOutElastic:
                    return InOutElastic(x);
                case EasingCurve.InBounce:
                    return 1 - OutBounce(1 - x);
                case EasingCurve.OutBounce:
                    return OutBounce(x);
                case EasingCurve.InOutBounce:
                    return x < 0.5 ? (1 - OutBounce(1 - 2 * x)) / 2 : (1 + OutBounce(2 * x - 1)) / 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(curve));
            }
        }

        internal static void RequireDefined(EasingCurve curve, string name)
        {
            if (curve < EasingCurve.Linear || curve > EasingCurve.InOutBounce)
                throw new ArgumentOutOfRangeException(name);
        }

        private static double InOutBack(double x)
        {
            if (x < 0.5) return Math.Pow(2 * x, 2) * ((BackInOutOvershoot + 1) * 2 * x - BackInOutOvershoot) / 2;

            return (Math.Pow(2 * x - 2, 2) * ((BackInOutOvershoot + 1) * (2 * x - 2) + BackInOutOvershoot) + 2) / 2;
        }

        private static double InOutElastic(double x)
        {
            var wave = Math.Sin((20 * x - 11.125) * ElasticInOutPeriod);
            if (x < 0.5) return -(Math.Pow(2, 20 * x - 10) * wave) / 2;

            return Math.Pow(2, -20 * x + 10) * wave / 2 + 1;
        }

        private static double OutBounce(double x)
        {
            const double strength = 7.5625;
            const double span = 2.75;
            if (x < 1 / span) return strength * x * x;
            if (x < 2 / span) return Bounce(x - 1.5 / span, 0.75);
            if (x < 2.5 / span) return Bounce(x - 2.25 / span, 0.9375);

            return Bounce(x - 2.625 / span, 0.984375);

            static double Bounce(double offset, double floor) => strength * offset * offset + floor;
        }
    }
}
