using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace TradeServer.Commands
{
    public class CommandOp : Command
    {
        public override string Name => "op";
        public override string Usage => "<target>";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);

            Packet announcePacket = new PacketAnnouncement
            {
                Message = "You are now an admin.\nThis allows you to execute privileged server commands in chat.",
                Type = PacketAnnouncement.MessageType.Dialog
            };

            foreach (var client in CommandUtility.GetClientsFromInput(args[0]))
            {
                client.PlayerInfo.Permission = PermissionLevel.Admin;
                client.PlayerInfo.Save();
                caller.Output($"{client.Player.Name} is now an admin.");

                client.SendPacket(announcePacket);
            }
        }
    }
}
