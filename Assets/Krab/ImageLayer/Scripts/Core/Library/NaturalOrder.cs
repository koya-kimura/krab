using System;
using System.Collections.Generic;

namespace Krab.ImageLayer.Core.Library
{
    /// <summary>
    /// Orders names so that digit runs compare by value ("2" before "10"). Equal values with more leading zeros
    /// sort later, and text runs compare case-insensitively first, so the order is total and stable.
    /// </summary>
    public sealed class NaturalOrder : IComparer<string>
    {
        public static readonly NaturalOrder Instance = new();

        private NaturalOrder()
        {
        }

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int tieBreak = 0;
            int indexX = 0;
            int indexY = 0;
            while (indexX < x.Length && indexY < y.Length)
            {
                bool digitX = char.IsDigit(x[indexX]);
                bool digitY = char.IsDigit(y[indexY]);
                int endX = RunEnd(x, indexX, digitX);
                int endY = RunEnd(y, indexY, digitY);

                int result;
                if (digitX && digitY)
                {
                    result = CompareNumbers(x, indexX, endX, y, indexY, endY, out int zeroTieBreak);
                    if (tieBreak == 0) tieBreak = zeroTieBreak;
                }
                else
                {
                    var runX = x.AsSpan(indexX, endX - indexX);
                    var runY = y.AsSpan(indexY, endY - indexY);
                    result = runX.CompareTo(runY, StringComparison.OrdinalIgnoreCase);
                    if (tieBreak == 0) tieBreak = runX.CompareTo(runY, StringComparison.Ordinal);
                }

                if (result != 0) return result;

                indexX = endX;
                indexY = endY;
            }

            int remaining = (x.Length - indexX).CompareTo(y.Length - indexY);
            if (remaining != 0) return remaining;

            return Math.Sign(tieBreak);
        }

        private static int RunEnd(string text, int start, bool digits)
        {
            int end = start;
            while (end < text.Length && char.IsDigit(text[end]) == digits)
                end++;

            return end;
        }

        private static int CompareNumbers(
            string x, int startX, int endX, string y, int startY, int endY, out int zeroTieBreak)
        {
            int significantX = SkipZeros(x, startX, endX);
            int significantY = SkipZeros(y, startY, endY);

            // More leading zeros sort later when the values are equal ("1" before "01").
            zeroTieBreak = (significantX - startX).CompareTo(significantY - startY);

            int lengthCompare = (endX - significantX).CompareTo(endY - significantY);
            if (lengthCompare != 0) return lengthCompare;

            return x.AsSpan(significantX, endX - significantX)
                .CompareTo(y.AsSpan(significantY, endY - significantY), StringComparison.Ordinal);
        }

        private static int SkipZeros(string text, int start, int end)
        {
            while (start < end - 1 && text[start] == '0')
                start++;

            return start;
        }
    }
}
