using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandHelp : Command
    {
        public override string Name => "help";
        public override string Usage => "";

        public override async Task Execute(Caller caller, string[] args)
        {
            var sb = new StringBuilder("RimLink Server Help");
            sb.AppendLine("\nAngle brackets indicate a required argument, normal brackets indicate an optional argument. Curly brackets indicate an optional flag.");
            sb.AppendLine();

            foreach (Command command in Program.Commands)
                sb.AppendLine($"  {command.Name} {command.Usage}");

            sb.AppendLine();

            caller.Output(sb.ToString());
        }
    }
}
