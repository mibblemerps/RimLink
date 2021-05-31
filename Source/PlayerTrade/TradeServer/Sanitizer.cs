using System;
using System.IO;

namespace TradeServer
{
    public static class Sanitizer
    {
        public static string SanitizeFileName(this string str)
        {
            return string.Join("_", str.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
