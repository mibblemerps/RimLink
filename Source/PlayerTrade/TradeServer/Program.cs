using System;
using System.Collections.Generic;
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

        public static List<Command> Commands = new List<Command>
        {
            new CommandTest(),
            new CommandList(),
            new CommandSendLetter(),
            new CommandKick(),
            new CommandAnnouncement(),
        };

        private static Caller ServerCaller = new ServerCaller();
        private static Task _serverTask;

        private static void Main(string[] args)
        {
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
            List<string> split = new List<string>(CommandUtility.SplitArguments(input));
            if (split.Count == 0)
                return; // no command
            string commandName = split.First();
            string[] args = split.Skip(1).ToArray();

            Command command = Commands.FirstOrDefault(cmd => cmd.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
            if (command == null)
            {
                ServerCaller.Error($"Command \"{commandName}\" not found!");
                return;
            }

            try
            {
                command.Execute(ServerCaller, args).Wait();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    if (e.InnerException is CommandException cmdException)
                    {
                        Log.Error(cmdException.Message);
                        return;
                    }
                }
                Log.Error($"Exception running command \"{command.Name}\"!", e);
            }
        }
    }
}
