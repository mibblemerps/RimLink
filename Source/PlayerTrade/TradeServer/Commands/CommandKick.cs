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

            Client target = CommandUtility.GetClientFromInput(args[0]);
            if (target == null)
                throw new CommandException("Player not found");

            await target.Disconnect();

            caller.Output("Kicked " + target.Player.Name);
        }
    }
}
