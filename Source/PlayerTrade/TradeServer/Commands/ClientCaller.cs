using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Chat;
using Verse;

namespace TradeServer.Commands
{
    public class ClientCaller : Caller
    {
        public Client Client;

        public override string Guid => Client.Player.Guid;
        public override bool IsAdmin => Program.Permissions.GetPermission(Guid) == ClientPermissions.PermissionLevel.Admin;

        public ClientCaller(Client client)
        {
            Client = client;
        }

        public override void Output(string output)
        {
            _ = Client.SendPacket(new PacketReceiveChatMessage
            {
                Messages = new List<PacketReceiveChatMessage.NetMessage>
                {
                    new PacketReceiveChatMessage.NetMessage
                    {
                        From = null,
                        Message = output
                    }
                }
            });
        }

        public override void Error(string error)
        {
            Output(error.Colorize(ColoredText.RedReadable));
        }
    }
}
