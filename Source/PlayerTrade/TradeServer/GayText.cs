using System;

namespace TradeServer
{
    public class GayText
    {
        private static ConsoleColor[] Colors = {
            ConsoleColor.DarkRed,
            ConsoleColor.DarkYellow,
            ConsoleColor.DarkGreen,
            ConsoleColor.Blue,
            ConsoleColor.Magenta
        };
        
        public static void Write(string text)
        {
            ConsoleColor original = Console.ForegroundColor;
            
            for (int i = 0; i < text.Length; i++)
            {
                Console.ForegroundColor = Colors[i % Colors.Length];
                Console.Write(text[i]);
            }

            Console.ForegroundColor = original;
        }

        public static void WriteLine(string text) => Write(text + "\n");
    }
}