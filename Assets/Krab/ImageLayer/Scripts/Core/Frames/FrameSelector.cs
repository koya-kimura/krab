using System;

namespace Krab.ImageLayer.Core.Frames
{
    /// <summary>Chooses which frame of a clip to show.</summary>
    public static class FrameSelector
    {
        /// <summary>
        /// Frame for a position measured in frames, e.g. seconds × fps or beats × frames per beat.
        /// Negative positions are allowed; Clamp holds the first frame there.
        /// </summary>
        public static int FromPosition(double position, int frameCount, FrameWrap wrap)
        {
            Require.Finite(position, nameof(position));
            Require.Positive(frameCount, nameof(frameCount));
            if (frameCount == 1) return 0;

            double step = Math.Floor(position);
            switch (wrap)
            {
                case FrameWrap.Clamp:
                    return (int)Math.Clamp(step, 0, frameCount - 1);
                case FrameWrap.Loop:
                    return (int)PositiveModulo(step, frameCount);
                case FrameWrap.PingPong:
                {
                    int period = 2 * (frameCount - 1);
                    int index = (int)PositiveModulo(step, period);
                    return index < frameCount ? index : period - index;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(wrap), wrap, null);
            }
        }

        /// <summary>
        /// Frame for a progress where 1 is one pass through the clip: 0..1 plays forward once. With PingPong,
        /// 1..2 plays back. Clamp shows the last frame at exactly 1.
        /// </summary>
        public static int FromProgress(double progress, int frameCount, FrameWrap wrap)
        {
            Require.Finite(progress, nameof(progress));
            Require.Positive(frameCount, nameof(frameCount));

            // A PingPong pass covers frameCount - 1 steps, so both end frames land on whole progress values.
            int framesPerPass = wrap == FrameWrap.PingPong ? frameCount - 1 : frameCount;
            return FromPosition(progress * framesPerPass, frameCount, wrap);
        }

        private static double PositiveModulo(double value, int modulus)
        {
            double result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
