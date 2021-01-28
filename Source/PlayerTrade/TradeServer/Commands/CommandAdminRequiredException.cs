using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandAdminRequiredException : CommandException
    {
        public CommandAdminRequiredException() : base("Admin required")
        {
        }
    }
}
