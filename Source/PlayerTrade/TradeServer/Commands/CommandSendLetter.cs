using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Mail;

namespace TradeServer.Commands
{
    public class CommandSendLetter : Command
    {
        public override string Name => "sendletter";

        public override async Task Execute(Caller caller, string[] args)
        {
            if (!caller.IsAdmin)
                throw new CommandException("Admin required");

            if (args.Length < 1)
                throw new CommandException("Invalid arguments");

            Client target = CommandUtility.GetClientFromInput(args[0]);
            if (target == null)
                throw new CommandException("Player not found");

            string title = "";
            if (args.Length >= 2)
                title = args[1];

            string body = "";
            if (args.Length >= 3)
                body = string.Join(" ", args.Skip(2).ToArray());

            await target.SendPacket(new PacketMail
            {
                For = target.Player.Guid,
                From = "Server",
                Title = title,
                Body = body
            });

            caller.Output("Letter sent");
        }
    }
}
