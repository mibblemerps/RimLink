using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace TradeServer.Commands
{
    public class CommandKick : Command
    {
        public override string Name => "kick";
        public override string Usage => "<target> (reason)";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);

            string reason = "Kicked from server.";
            if (args.Length > 1) // custom reason
                reason = string.Join(" ", args.Skip(1));

            foreach (Client client in CommandUtility.GetClientsFromInput(args[0]))
            {
                _ = client.SendPacketDirect(new PacketKick
                {
                    Reason = reason,
                    AllowReconnect = false,
                }).ContinueWith(t => { client.Disconnect(DisconnectReason.Kicked); });
                caller.Output("Kicked " + client.Player.Name);
            }
        }
    }
}
