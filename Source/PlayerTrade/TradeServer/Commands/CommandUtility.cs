﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;
using Log = RimLink.Log;

namespace TradeServer.Commands
{
    public static class CommandUtility
    {
        public static void ExecuteCommand(Caller caller, string input)
        {
            List<string> split = new List<string>(SplitArguments(input.TrimStart('/')));
            if (split.Count == 0)
                return; // no command
            string commandName = split.First();
            string[] args = split.Skip(1).ToArray();

            Command command = Program.Commands.FirstOrDefault(cmd => cmd.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
            if (command == null)
            {
                caller.Error($"Command \"{commandName}\" not found!");
                return;
            }

            try
            {
                command.Execute(caller, args).Wait();
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    if (e.InnerException is CommandException cmdException)
                    {
                        caller.Error(cmdException.Message);
                        return;
                    }
                }
                Log.Error($"Exception running command \"{command.Name}\"!", e);
            }
        }

        public static string[] SplitArguments(string input)
        {
            return Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(match => match.Value.Trim('"'))
                .ToArray();
        }

        public static Client GetClientFromInput(string input, bool throwOnNotFound = false)
        {
            Client client = GetClientsFromInput(input).FirstOrDefault();

            if (client == null && throwOnNotFound)
                throw new CommandException($"Player \"{input}\" couldn't found!");
            
            return client;
        }

        public static IEnumerable<Client> GetClientsFromInput(string input, bool throwOnNoneFound = true)
        {
            input = input.Trim();

            if (input.Equals("@a", StringComparison.InvariantCultureIgnoreCase))
            {
                // All players
                foreach (Client client in Program.Server.AuthenticatedClients)
                    yield return client;

                yield break;
            }

            if (input.Equals("@r", StringComparison.InvariantCultureIgnoreCase))
            {
                // Random player
                yield return Program.Server.AuthenticatedClients.RandomElement();
                yield break;
            }

            for (int guidLength = input.Length; guidLength >= 4; guidLength--)
            {
                string shortGuid = input.Substring(0, guidLength);
                foreach (Client client in Program.Server.AuthenticatedClients)
                {
                    if (client.Player.Guid.Substring(0, guidLength)
                        .Equals(shortGuid, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // GUID match - don't find multiple since the user would be intended for one result
                        yield return client;
                        yield break;
                    }
                }
            }

            Client nameClient = Program.Server.AuthenticatedClients.FirstOrDefault(client =>
                client.Player.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase));
            if (nameClient != null)
            {
                yield return nameClient;
                yield break;
            }

            if (throwOnNoneFound)
                throw new CommandException("No players found with selector: " + input);
        }

        public static string GetClientsStringFromInput(string input)
        {
            var list = new List<Client>(GetClientsFromInput(input));
            
            if (list.Count == 0)
                return "no-one";
            
            if (list.Count == 1)
                return list.First().Player.ToString();

            return $"{list.Count} players";
        }

        public static void AdminRequired(Caller caller)
        {
            if (!caller.IsAdmin)
                throw new CommandAdminRequiredException();
        }

        public static bool TryParseTimeSpan(string[] args, out TimeSpan timeSpan)
        {
            if (args.Length < 2)
            {
                // Not enough args
                timeSpan = TimeSpan.Zero;
                return false;
            }

            if (!double.TryParse(args[0], out double num))
            {
                // First arg NaN
                timeSpan = TimeSpan.Zero;
                return false;
            }
            
            string unit = args[1].ToLower().TrimEnd('s', ' ', '.');
            switch (unit)
            {
                case "second":
                case "sec":
                    timeSpan = TimeSpan.FromSeconds(num);
                    return true;
                case "minute":
                case "min":
                    timeSpan = TimeSpan.FromMinutes(num);
                    return true;
                case "hour":
                case "hr":
                    timeSpan = TimeSpan.FromHours(num);
                    return true;
                case "day":
                    timeSpan = TimeSpan.FromDays(num);
                    return true;
            }

            timeSpan = TimeSpan.Zero;
            return false;
        }

        public static bool ParseBoolean(string str)
        {
            return str.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
                   str.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
                   str.Equals("on", StringComparison.InvariantCultureIgnoreCase) ||
                   str.Equals("enable", StringComparison.InvariantCultureIgnoreCase) ||
                   str.Equals("1", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
