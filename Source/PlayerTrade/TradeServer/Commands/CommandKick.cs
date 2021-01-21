using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandKick : Command
    {
        public override string Name => "kick";

        public override async Task Execute(Caller caller, string[] args)
        {
            if (!caller.IsAdmin)
                throw new CommandException("Admin required");

            if (args.Length < 1)
                throw new CommandException("Invalid arguments");


            foreach (Client client in CommandUtility.GetClientsFromInput(args[0]))
            {
                await client.Disconnect();
                caller.Output("Kicked " + client.Player.Name);
            }
        }
    }
}
