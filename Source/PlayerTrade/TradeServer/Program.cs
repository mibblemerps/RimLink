using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using TradeServer.Commands;

namespace TradeServer
{
    public class Program
    {
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
        };

        private static Caller ServerCaller = new ServerCaller();
        private static Task _serverTask;

        private static void Main(string[] args)
        {
            Stopwatch.Start();

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("=== RimWorld Trade Server ===\n");
            Console.ResetColor();

            int port = 35562;
            Console.WriteLine("    Port: " + port);
            Console.WriteLine();

            Server = new Server();
            _serverTask = Server.Run(port);
            _serverTask.ContinueWith((t) =>
            {
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
    }
}
