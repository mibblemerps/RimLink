using System;

namespace TradeServer
{
    public static class TimeFormatter
    {
        public static string ToHumanString(this TimeSpan timeSpan, string eternalString = null)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{Math.Round((float) timeSpan.TotalSeconds, 1)} seconds";
            if (timeSpan.TotalMinutes < 60)
                return $"{Math.Round((float) timeSpan.TotalMinutes, 1)} minutes";
            if (timeSpan.TotalHours < 48)
                return $"{Math.Round((float) timeSpan.TotalHours, 1)} hours";
            if (eternalString != null && timeSpan.TotalDays > 36500) // if over 100 years, just assume forever
                return eternalString;
            return $"{Math.Round((float) timeSpan.TotalDays, 1)} days";
        }
    }
}
