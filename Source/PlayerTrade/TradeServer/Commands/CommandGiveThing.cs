using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;

namespace TradeServer.Commands
{
    public class CommandGiveThing : Command
    {
        public override string Name => "give";
        public override async Task Execute(Caller caller, string[] args)
        {
            if (!caller.IsAdmin)
                throw new CommandException("Admin required");

            // give <target> <thing> <stuff> 10 50% awful

            if (args.Length <= 0)
                throw new CommandException("Invalid usage");

            Client[] targets = new Client[0];

            PacketGiveItem packet = new PacketGiveItem
            {
                Reference = Guid.NewGuid().ToString(),
                Count = 1,
                HealthPercentage = 1f,
            };

            for (int i = 0; i < args.Length; i++)
            {
                if (i == 0)
                {
                    // First arg - target
                    targets = CommandUtility.GetClientsFromInput(args[i]).ToArray();
                    continue;
                }

                if (i == 1)
                {
                    // Second arg - thing def name
                    packet.DefName = args[i];
                    continue;
                }

                bool isNumber = int.TryParse(args[i].TrimEnd('%'), out int numVal);

                if (i == 1 && !isNumber)
                {
                    // Second arg non number - stuff def name
                    packet.StuffDefName = args[i];
                    continue;
                }

                if (isNumber)
                {
                    if (args[i].EndsWith("%"))
                    {
                        // Hitpoints arg
                        packet.HealthPercentage = numVal / 100f;
                    }
                    else
                    {
                        // Count arg
                        packet.Count = numVal;
                    }
                }
                else
                {
                    // Quality category
                    string[] qualities = Enum.GetNames(typeof(QualityCategory));
                    var options = qualities
                        .Where(s => s.StartsWith(args[i], StringComparison.InvariantCultureIgnoreCase)).ToArray();
                    if (options.Length == 1)
                    {
                        if (Enum.TryParse(options.First(), true, out QualityCategory qualityArg))
                            packet.Quality = qualityArg;
                    }
                    else if (options.Length > 1)
                    {
                        throw new CommandException(
                            $"Quality \"{args[i]}\" not found! Did you mean: {string.Join(", ", options)}?");
                    }
                    else
                    {
                        throw new CommandException($"Quality \"{args[i]}\" not found!");
                    }
                }
            }

            foreach (Client target in targets)
            {
                await target.SendPacket(packet);
                caller.Output($"Given thing to {target.Player.Name}");
            }
        }
    }
}
