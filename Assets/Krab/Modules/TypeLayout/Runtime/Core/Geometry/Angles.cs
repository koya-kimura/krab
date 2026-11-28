using System;

namespace Krab.TypeLayout.Core.Geometry
{
    /// <summary>Degrees on screen: 0 points right, positive turns clockwise (Y down).</summary>
    public static class Angles
    {
        public static double ToRadians(double degrees)
        {
            Require.Finite(degrees, nameof(degrees));
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            Require.Finite(radians, nameof(radians));
            return radians * (180 / Math.PI);
        }

        /// <summary>Wraps to (-180, 180].</summary>
        public static double Normalize(double degrees)
        {
            Require.Finite(degrees, nameof(degrees));
            var wrapped = degrees % 360;
            if (wrapped <= -180) wrapped += 360;
            else if (wrapped > 180) wrapped -= 360;

            return wrapped;
        }

        /// <summary>Cosine and sine, exact for multiples of 90 so axis-aligned layouts stay on whole pixels.</summary>
        public static (double Cos, double Sin) CosSin(double degrees)
        {
            Require.Finite(degrees, nameof(degrees));
            var wrapped = degrees % 360;
            if (wrapped < 0) wrapped += 360;

            switch (wrapped)
            {
                case 0:
                    return (1, 0);
                case 90:
                    return (0, 1);
                case 180:
                    return (-1, 0);
                case 270:
                    return (0, -1);
            }

            var radians = ToRadians(wrapped);
            return (Math.Cos(radians), Math.Sin(radians));
        }
    }
}
