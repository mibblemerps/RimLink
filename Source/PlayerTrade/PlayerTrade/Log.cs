using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade
{
    public static class Log
    {
        public const int InnerExceptionNestLimit = 8;

        public static bool Enabled = true;
        public static bool VerboseEnabled = true;

        public static bool RunningInRimWorld => RimLinkMod.Instance != null;

        private static int _innerExceptionCounter;

        public static void Verbose(string message, bool ignoreLimit = false)
        {
            if (!Enabled || !VerboseEnabled) return;

            if (RunningInRimWorld)
            {
                Verse.Log.Message("[RimLink] [Verbose] " + message, ignoreLimit);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[{DateTime.Now}] [INFO] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Message(string message, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (RunningInRimWorld)
            {
                Verse.Log.Message("[RimLink] [Info] " + message, ignoreLimit);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"[{DateTime.Now}] [INFO] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Warn(string message, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (RunningInRimWorld)
            {
                Verse.Log.Warning("[RimLink] [Warn] " + message, ignoreLimit);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"[{DateTime.Now}] [WARN] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Error(string message, Exception context = null, bool ignoreLimit = false)
        {
            if (!Enabled) return;

            if (_innerExceptionCounter >= InnerExceptionNestLimit)
            {
                _innerExceptionCounter = 0;
                Error($"Reached inner exception nesting limit! ({InnerExceptionNestLimit})");
                return;
            }

            if (context != null)
                message += $"\n{context.Message}\n{context.StackTrace}\n";

            if (RunningInRimWorld)
            {
                Verse.Log.Error("[RimLink] [Error] " + message, ignoreLimit);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[{DateTime.Now}] [ERROR] ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            // Log inner exception also
            if (context != null && context.InnerException != null)
            {
                _innerExceptionCounter++;
                Error($"InnerException: {context.InnerException.Message}\n{context.InnerException.StackTrace}\n");
            }
            else
            {
                _innerExceptionCounter = 0;
            }
        }
    }
}
