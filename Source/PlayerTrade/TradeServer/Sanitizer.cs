using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
