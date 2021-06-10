using System.Threading.Tasks;
#pragma warning disable 1998

namespace TradeServer.Commands
{
    public class CommandSendDelay : Command
    {
        public override string Name => "senddelay";

        public override string Usage => "<target> <seconds>";
        
        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length == 0)
                throw new CommandUsageException(this);

            float seconds = 0;
            if (args.Length >= 2 && float.TryParse(args[1], out float parsed))
                seconds = parsed;

            foreach (var client in CommandUtility.GetClientsFromInput(args[0]))
            {
                client.ArtificialSendDelay = seconds;
            }

            caller.Output(seconds == 0
                ? "Removed send delay."
                : "Applied send delay.");
        }
    }
}