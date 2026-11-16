using System;

namespace Krab.TypeLayout.Core
{
    internal static class Require
    {
        public static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, value, "Value must be finite.");

            return value;
        }

        public static double NonNegative(double value, string name)
        {
            Finite(value, name);
            if (value < 0) throw new ArgumentOutOfRangeException(name, value, "Value must not be negative.");

            return value;
        }

        public static double Positive(double value, string name)
        {
            Finite(value, name);
            if (value <= 0) throw new ArgumentOutOfRangeException(name, value, "Value must be greater than zero.");

            return value;
        }

        public static int Positive(int value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name, value, "Value must be greater than zero.");

            return value;
        }

        public static T NotNull<T>(T value, string name) where T : class
        {
            if (value == null) throw new ArgumentNullException(name);

            return value;
        }
    }
}
