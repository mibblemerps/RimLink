using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace TradeServer.Commands
{
    public class CommandDeop : Command
    {
        public override string Name => "deop";
        public override string Usage => "<target>";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);

            Packet announcePacket = new PacketAnnouncement
            {
                Message = "You are no longer an admin.",
                Type = PacketAnnouncement.MessageType.Message
            };

            foreach (var client in CommandUtility.GetClientsFromInput(args[0]))
            {
                if (client.PlayerInfo.Permission != PermissionLevel.Admin)
                {
                    caller.Error($"{client.Player.Name} is not an admin!");
                    continue;
                }

                client.PlayerInfo.Permission = PermissionLevel.Player;
                client.PlayerInfo.Save();
                caller.Output($"{client.Player.Name} is no longer an admin.");

                client.SendPacket(announcePacket);
            }
        }
    }
}
