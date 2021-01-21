using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Chat;

namespace TradeServer.Commands
{
    public class CommandSay : Command
    {
        public override string Name => "say";
        public override async Task Execute(Caller caller, string[] args)
        {
            if (!caller.IsAdmin)
                throw new CommandException("Requires admin");

            string msg = string.Join(" ", args);
            var packet = new PacketReceiveChatMessage
            {
                Messages = new List<PacketReceiveChatMessage.NetMessage>
                {
                    new PacketReceiveChatMessage.NetMessage
                    {
                        From = null, // (from server)
                        Message = msg
                    }
                }
            };

            foreach (Client client in Program.Server.AuthenticatedClients)
            {
                await client.SendPacket(packet);
            }

            Log.Message("[Chat] Server: " + msg);
        }
    }
}
