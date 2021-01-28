using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandUsageException : CommandException
    {
        public CommandUsageException(Command command) : base($"Usage: {command.Name} {command.Usage}")
        {
        }
    }
}
