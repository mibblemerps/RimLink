using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public static class CommandUtility
    {
        public static string[] SplitArguments(string input)
        {
            return Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(match => match.Value.Trim('"'))
                .ToArray();
        }

        public static Client GetClientFromInput(string input, bool throwOnNotFound = false)
        {
            input = input.Trim();

            for (int guidLength = input.Length; guidLength >= 4; guidLength--)
            {
                string shortGuid = input.Substring(0, guidLength);
                foreach (Client client in Program.Server.AuthenticatedClients)
                {
                    if (client.Player.Guid.Substring(0, guidLength).Equals(shortGuid, StringComparison.InvariantCultureIgnoreCase))
                        return client;
                }
            }

            Client nameClient = Program.Server.AuthenticatedClients.FirstOrDefault(client =>
                client.Player.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase));
            if (nameClient != null)
                return nameClient;

            if (throwOnNotFound)
                throw new CommandException($"Player \"{input}\" couldn't found!");
            return null;
        }
    }
}
