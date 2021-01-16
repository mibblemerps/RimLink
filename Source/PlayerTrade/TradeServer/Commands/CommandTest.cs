using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandTest : Command
    {
        public override string Name => "test";
        public override async Task Execute(Caller caller, string[] args)
        {
            if (args.Contains("exception") || args.Contains("ex"))
                throw new CommandException("Test exception");
            caller.Output("Hello world!");
        }
    }
}
