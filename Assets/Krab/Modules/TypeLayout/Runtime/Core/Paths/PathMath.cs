namespace Krab.TypeLayout.Core.Paths
{
    internal static class PathMath
    {
        /// <summary>Wraps into [0, length).</summary>
        public static double Wrap(double distance, double length)
        {
            var wrapped = distance % length;
            if (wrapped < 0) wrapped += length;

            // -tiny % length can round up to length itself.
            return wrapped >= length ? 0 : wrapped;
        }
    }
}
