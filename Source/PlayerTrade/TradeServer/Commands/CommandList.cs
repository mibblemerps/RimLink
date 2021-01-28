using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public class CommandList : Command
    {
        public override string Name => "list";
        public override string Usage => "{more}";

        public override async Task Execute(Caller caller, string[] args)
        {
            bool verbose = args.Length >= 1 && args[0].Equals("more", StringComparison.InvariantCultureIgnoreCase);

            var sb = new StringBuilder($"{Server.AuthenticatedClients.Count} players online. ");

            if (verbose)
                sb.AppendLine();

            bool first = true;
            foreach (Client client in Server.AuthenticatedClients)
            {
                if (verbose)
                {
                    sb.AppendLine($" - {client.Player.Name} (Tradeable = {(client.Player.TradeableNow ? "Yes" : "No")})");
                    sb.AppendLine($"       Guid: {client.Player.Guid}");
                    sb.AppendLine($"       Day: {client.Player.Day}, Weather: {client.Player.Weather}, Wealth: {client.Player.Wealth}");
                    if (caller.IsAdmin)
                        sb.AppendLine($"       IP: {client.Tcp.Client.RemoteEndPoint}");
                }
                else
                {
                    sb.Append((first ? "" : ", ") + client.Player.Name);
                }

                first = false;
            }
            

            caller.Output(sb.ToString());
        }
    }
}
