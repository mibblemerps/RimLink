using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace TradeServer.Commands
{
    public class CommandBan : Command
    {
        public override string Name => "ban";
        public override string Usage => "<target> (<time> seconds|minutes|hours|days) (reason)";

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);

            string targetStr = args[0];
            PlayerInfo target = null;

            Client onlineClient = CommandUtility.GetClientFromInput(targetStr);
            if (onlineClient != null)
                target = onlineClient.PlayerInfo;
            
            if (target == null)
            {
                // No target found yet - try to find offline client by GUID
                target = PlayerInfo.Load(targetStr.ToLower(), false);
            }

            if (target == null)
            {
                caller.Error("Player not found. If they're offline, use their (exact, non-truncated) GUID instead.");
                return;
            }

            DateTime banExpiry = DateTime.MaxValue;
            string reason = string.Join(" ", args.Skip(1).ToArray());

            // Read ban expiry
            if (CommandUtility.TryParseTimeSpan(args.Skip(1).ToArray(), out TimeSpan timeSpan))
            {
                banExpiry = DateTime.Now + timeSpan;

                // Skip ban expiry arguments from reason
                reason = string.Join(" ", args.Skip(3).ToArray());
            }

            if (string.IsNullOrWhiteSpace(reason))
                reason = null;

            // Issue ban
            target.BannedUntil = banExpiry;
            target.BanReason = reason;
            target.Save();

            // Kick target if they're online
            if (onlineClient != null)
            {
                string kickReason = "You've been banned from this server.";
                if (!string.IsNullOrWhiteSpace(reason))
                    kickReason += "\n\nReason: " + reason;
                if (timeSpan > TimeSpan.Zero && timeSpan < TimeSpan.FromDays(36500))
                    kickReason += $"\n\nYour ban will expire in {timeSpan.ToHumanString()}.";

                await onlineClient.SendPacketDirect(new PacketKick
                {
                    Reason = kickReason,
                    AllowReconnect = false,
                });
                onlineClient.Disconnect();
            }

            caller.Output($"Player {target.Guid} has been banned for {(banExpiry == DateTime.MaxValue ? "forever" : timeSpan.ToHumanString())}. Reason: {reason}");
        }
    }
}
