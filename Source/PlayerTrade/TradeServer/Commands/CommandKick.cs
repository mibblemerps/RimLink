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
        public override string Usage => "<target>";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);
            
            foreach (Client client in CommandUtility.GetClientsFromInput(args[0]))
            {
                await client.Disconnect();
                caller.Output("Kicked " + client.Player.Name);
            }
        }
    }
}
