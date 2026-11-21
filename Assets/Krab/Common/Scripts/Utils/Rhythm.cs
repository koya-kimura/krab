using UnityEngine;

namespace Krab.Common.Utils
{
    public delegate float EasingFn(float x);
    
    public static class Rhythm
    {
        // Keeps zero or negative lengths from dividing by zero; fractional beats such as 0.25 stay usable.
        private const float MinLength = 1e-4f;

        public static int GetIndex(float t, float interval)
        {
            return Mathf.FloorToInt(Mathf.Max(0f, t) / Mathf.Max(MinLength, interval));
        }

        /// <summary>
        /// Cycle count that holds, then eases to the next whole number over the last <paramref name="easeDuration"/>
        /// of each cycle. An ease as long as the cycle gives a continuous count.
        /// </summary>
        public static float GetPhase(
            float t,
            float cycleLength,
            float easeDuration,
            EasingFn easingFn = null)
        {
            cycleLength = Mathf.Max(MinLength, cycleLength);
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeProgress = EaseProgress(localTime, cycleLength, easeDuration);
            var easing = easingFn ?? Easing.EaseInOutSine;

            return Mathf.Lerp(
                currentCycle,
                nextCycle,
                easing(easeProgress)
            );
        }

        public static float GetLerpNoise(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0,
            EasingFn easingFn = null)
        {
            cycleLength = Mathf.Max(MinLength, cycleLength);
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeProgress = EaseProgress(localTime, cycleLength, easeDuration);
            var easing = easingFn ?? Easing.EaseInOutSine;

            var currentNoise = Pcg.Pcg01(
                new Vector2(seed, currentCycle)
            ).x;

            var nextNoise = Pcg.Pcg01(
                new Vector2(seed, nextCycle)
            ).x;

            return Mathf.Lerp(
                currentNoise,
                nextNoise,
                easing(easeProgress)
            );
        }

        public static Vector3 GetLerpNoise3D(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0,
            EasingFn easingFn = null)
        {
            var newSeed = Pcg.Hash4D(
                new UInt4(seed, seed, seed, seed)
            );

            var time =
                t +
                Pcg.Pcg01(newSeed.W) * cycleLength;

            return new Vector3(
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.X,
                    easingFn
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Y,
                    easingFn
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Z,
                    easingFn
                ) * 2f - 1f
            );
        }

        private static float EaseProgress(float localTime, float cycleLength, float easeDuration)
        {
            // No ease: jump at the cycle boundary instead of dividing by zero.
            if (easeDuration <= 0f) return 0f;

            easeDuration = Mathf.Min(easeDuration, cycleLength);
            var easeStartTime = cycleLength - easeDuration;
            return Mathf.Clamp01((localTime - easeStartTime) / easeDuration);
        }
    }
}