using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade
{
    public static class Log
    {
        public static bool Enabled = true;
        public static bool RunningInRimWorld => PlayerTradeMod.Instance != null;

        public static void Message(string message, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (RunningInRimWorld)
                Verse.Log.Message(message, ignoreLimit);
            else
                Console.WriteLine($"[{DateTime.Now}] [INFO]  {message}");
        }

        public static void Warn(string message, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (RunningInRimWorld)
                Verse.Log.Warning(message, ignoreLimit);
            else
                Console.WriteLine($"[{DateTime.Now}] [WARN]  {message}");
        }

        public static void Error(string message, Exception context = null, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (context != null)
                message += $"\n{context.Message}\n{context.StackTrace}\n";
            if (context != null && context.InnerException != null)
                message += $"(InnerException) {context.InnerException.Message}\n{context.InnerException.StackTrace}\n";

            if (RunningInRimWorld)
                Verse.Log.Error(message, ignoreLimit);
            else
                Console.WriteLine($"[{DateTime.Now}] [ERROR] {message}");
        }
    }
}
