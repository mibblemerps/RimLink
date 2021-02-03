using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace TradeServer.Commands
{
    public class CommandAnnouncement : Command
    {
        public override string Name => "announce";
        public override string Usage => "<message>";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length == 0)
                throw new CommandUsageException(this);

            int startIndex = 0;
            PacketAnnouncement.MessageType type = PacketAnnouncement.MessageType.Dialog;

            if (args[0] == "dialog")
            {
                type = PacketAnnouncement.MessageType.Dialog;
                startIndex++;
            }
            else if (args[0] == "message")
            {
                type = PacketAnnouncement.MessageType.Message;
                startIndex++;
            }

            string body = string.Join(" ", args.Skip(startIndex).ToArray());

            var packet = new PacketAnnouncement
            {
                Message = body,
                Type = type
            };
            foreach (Client client in Server.AuthenticatedClients)
            {
                client.SendPacket(packet);
            }

            caller.Output("Sent announcement");
        }
    }
}
