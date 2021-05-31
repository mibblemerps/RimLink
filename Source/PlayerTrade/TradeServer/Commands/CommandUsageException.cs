namespace TradeServer.Commands
{
    public class CommandUsageException : CommandException
    {
        public CommandUsageException(Command command) : base($"Usage: {command.Name} {command.Usage}")
        {
        }
    }
}
