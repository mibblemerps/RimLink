using System;
using Verse;

namespace RimLink.Util
{
    public static class StringUtil
    {
        public static string FormatIntRangeNice(this IntRange range, string separator = " - ", int minPlusThreshold = 9999)
        {
            if (range.min == range.max)
                return range.min.ToString();

            if (range.max >= minPlusThreshold)
                return range.min + "+";

            return range.min + separator + range.max;
        }

        public static string FormatFloatRangeNice(this FloatRange range, string separator = " - ", int decimals = 2, int minPlusThreshold = 9999)
        {
            if (Math.Abs(range.min - range.max) < 0.01f)
                return Math.Round(range.min, decimals).ToString();

            if (range.max >= minPlusThreshold)
                return Math.Round(range.min, decimals) + "+";

            return range.min + separator + range.max;
        }

        public static string MaybeS(this int num)
        {
            return num == 1 ? "" : "s";
        }

        public static string MaybeS(this float num, float tolerance = 0.01f)
        {
            return Math.Abs(num - 1) < tolerance ? "" : "s";
        }
    }
}
