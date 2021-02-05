using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer
{
    public static class TimeFormatter
    {
        public static string ToHumanString(this TimeSpan timeSpan, string eternalString = null)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.TotalSeconds} seconds";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.TotalMinutes} minutes";
            if (timeSpan.TotalHours < 48)
                return $"{timeSpan.TotalHours} hours";
            if (eternalString != null && timeSpan.TotalDays > 36500) // if over 100 years, just assume forever
                return eternalString;
            return $"{timeSpan.TotalDays} days";
        }
    }
}
