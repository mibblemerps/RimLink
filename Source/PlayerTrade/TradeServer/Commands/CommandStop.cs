using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using UnityEngine;

namespace TradeServer.Commands
{
    public class CommandStop : Command
    {
        public override string Name => "stop";
        public override string Usage => "(countdown)|cancel";

        private CancellationTokenSource _cancel;

        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);

            if (args.Length >= 1 && args[0].Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
            {
                // Cancel stop
                if (_cancel == null)
                {
                    caller.Error("Server wasn't shutting down!");
                }
                else
                {
                    _cancel.Cancel();
                    caller.Output("Server shutdown cancelled.");
                }
                return;
            }

            float countdown = 0f;
            if (args.Length >= 1)
            {
                if (!float.TryParse(args[0], out countdown))
                    throw new CommandUsageException(this);
            }

            caller.Output(countdown < 0.001f
                ? "Shutting down server..."
                : $"Server will shutdown in {countdown} seconds...");

            SendAnnouncement(countdown);

            _cancel = new CancellationTokenSource();
            _ = Task.Delay(Mathf.RoundToInt(countdown * 1000f), _cancel.Token).ContinueWith((t) =>
            {
                if (_cancel.IsCancellationRequested)
                    return; // cancelled

                _cancel = null;

                caller.Output("Goodbye");
                Process.GetCurrentProcess().Kill(); // todo: maybe a more graceful method
            });
        }

        private void SendAnnouncement(float seconds)
        {
            string msg = "Server is shutting down...";
            if (seconds <= 300)
                msg = $"Server is shutting down in {seconds} seconds...";
            else if (seconds > 300)
                msg = $"Server is shutting down in {Mathf.FloorToInt(seconds / 60f)} minutes...";

            Packet packet = new PacketAnnouncement
            {
                Message = msg,
                Type = PacketAnnouncement.MessageType.Dialog
            };

            foreach (var client in Program.Server.AuthenticatedClients)
                client.SendPacket(packet);
        }
    }
}
