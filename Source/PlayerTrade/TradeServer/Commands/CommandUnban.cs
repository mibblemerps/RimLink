using System.Threading.Tasks;
#pragma warning disable 1998

namespace TradeServer.Commands
{
    public class CommandUnban : Command
    {
        public override string Name => "unban";
        public override string Usage => "<target>";
        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length < 1)
                throw new CommandUsageException(this);

            PlayerInfo target = PlayerInfo.Load(args[0].ToLower(), false);
            if (target == null)
            {
                caller.Error("Player not found. Please make sure you use their (exact, non-truncated) GUID.");
                return;
            }

            target.BannedUntil = null;
            target.BanReason = null;
            target.Save();

            caller.Output($"Player {target.Guid} has been unbanned.");
        }
    }
}
