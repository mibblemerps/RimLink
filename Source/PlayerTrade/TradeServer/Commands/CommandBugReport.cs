using System.Linq;
using System.Threading.Tasks;
using RimLink;
using RimLink.Net.Packets;

namespace TradeServer.Commands
{
    public class CommandBugReport : Command
    {
        public override string Name => "bug";
        public override string Usage => "(target)";

        public override async Task Execute(Caller caller, string[] args)
        {
            Client[] targets;
            if (args.Length == 0)
            {
                // No args, target self
                Client self = Server.GetClient(caller.Guid);
                if (self == null)
                    throw new CommandException("Player not specified.");
                targets = new[] {self};
                caller.Output("Generating bug report. Thank you.");
            }
            else if (args.Length == 1)
            {
                // 1 arg, target specific player
                CommandUtility.AdminRequired(caller);
                targets = CommandUtility.GetClientsFromInput(args[0]).ToArray();
                if (targets.Length == 1)
                    caller.Output($"Requested bug report from {targets[0].Player.Name} ({targets[0].Player.Guid}).");
                else
                    caller.Output("Request bug reports.");
            }
            else
            {
                throw new CommandUsageException(this);
            }

            foreach (Client client in targets)
            {
                Log.Message($"Requesting bug report from {client.Player.Name} ({client.Player.Guid})...");
                client.SendPacket(new PacketRequestBugReport());
            }
        }
    }
}
