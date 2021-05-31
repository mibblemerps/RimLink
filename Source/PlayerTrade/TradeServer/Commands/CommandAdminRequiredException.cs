namespace TradeServer.Commands
{
    public class CommandAdminRequiredException : CommandException
    {
        public CommandAdminRequiredException() : base("Admin required")
        {
        }
    }
}
