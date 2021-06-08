using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PlayerTrade;
using TradeServer.Commands;

namespace TradeServer
{
    public class Program
    {
        private const string DefaultRimWorldAssemblyPath = @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll";
        
        public static Server Server;

        public static Stopwatch Stopwatch = new Stopwatch();

        public static List<Command> Commands = new List<Command>
        {
            new CommandHelp(),
            new CommandStop(),
            new CommandList(),
            new CommandOp(),
            new CommandDeop(),
            new CommandSendLetter(),
            new CommandKick(),
            new CommandAnnouncement(),
            new CommandGiveThing(),
            new CommandSay(),
            new CommandBugReport(),
            new CommandBan(),
            new CommandUnban(),
            new CommandPacketIds(),
            new CommandSendDelay(),
            new CommandDevMode(),
        };

        private static Caller ServerCaller = new ServerCaller();
        private static Task _serverTask;

        private static void Main(string[] args)
        {
            // Check that RimWorld assembly is present
            if (!CheckForRimWorldAssembly()) return;
            
            Stopwatch.Start();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("=== RimWorld Trade Server ===\n");
            Console.ResetColor();

            Server = new Server();
            _serverTask = Server.Run(Server.ServerSettings.Port);
            Console.WriteLine("    Port: " + Server.ServerSettings.Port);
            Console.WriteLine();
            _serverTask.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    Log.Error("Server crashed.", t.Exception);
                    if (t.Exception?.InnerException != null)
                        Log.Error("Inner exception", t.Exception.InnerException);
                }
                Log.Message("Server stopped.");
            });

            while (true)
                ReadCommand();
        }

        public static void ReadCommand()
        {
            string input = Console.ReadLine();
            CommandUtility.ExecuteCommand(ServerCaller, input);
        }

        private static bool CheckForRimWorldAssembly()
        {
            if (File.Exists("Assembly-CSharp.dll")) return true;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Missing RimWorld assembly (Assembly-CSharp.dll)! ");
            Console.ResetColor();
            Console.WriteLine("This cannot be distributed with the server for legal reasons.\n");

            if (File.Exists(DefaultRimWorldAssemblyPath))
            {
                // Found assembly in Steam dir
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("Automatically found RimWorld assembly: ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(DefaultRimWorldAssemblyPath);
                Console.ResetColor();
                File.Copy(DefaultRimWorldAssemblyPath, "Assembly-CSharp.dll");
                Console.WriteLine("\nAssembly has been copied. Please restart the server.");
            }
            else
            {
                // Cannot find assembly - prompt user to copy it themself
                Console.Write("You can find it in ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(@"<RimWorld Install Dir>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll");
                Console.ResetColor();
                Console.WriteLine("Please copy this file into the server directory, then restart the server.");
            }

            Thread.Sleep(100);
            Console.WriteLine("\n\nPress any key to close...");
            Console.ReadKey();
            return false;
        }
    }
}
