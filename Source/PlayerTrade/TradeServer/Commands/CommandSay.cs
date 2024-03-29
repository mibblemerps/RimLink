﻿using System.Collections.Generic;
using System.Threading.Tasks;
using RimLink;
using RimLink.Systems.Chat;
#pragma warning disable 1998

namespace TradeServer.Commands
{
    public class CommandSay : Command
    {
        public override string Name => "say";
        public override string Usage => "(message)";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

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
                client.SendPacket(packet);
            }

            Log.Message("[Chat] Server: " + msg);
        }
    }
}
