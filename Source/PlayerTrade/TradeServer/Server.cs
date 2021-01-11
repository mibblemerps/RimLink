using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Net;

namespace TradeServer
{
    public class Server
    {
        public static int ProtocolVersion = 1;

        public List<Client> AuthenticatedClients = new List<Client>();
        public QueuedPacketStorage QueuedPacketStorage = new QueuedPacketStorage();

        protected TcpListener Listener;

        public async Task Run(int port = 35562)
        {
            QueuedPacketStorage.Load();

            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();

            while (true)
            {
                TcpClient tcp = await Listener.AcceptTcpClientAsync();
                Log.Message($"Accepted TCP connection from {tcp.Client.RemoteEndPoint}");
                var client = new Client(tcp);
                client.Authenticated += ClientOnAuthenticated;
                client.Disconnected += ClientOnDisconnected;
                client.ColonyInfoReceived += ClientOnColonyInfoReceived;
                _ = client.Run();
            }
        }

        public Client GetClient(string guid)
        {
            return AuthenticatedClients.FirstOrDefault(client => client.Player.Guid == guid);
        }

        public string GetName(string guid)
        {
            var client = GetClient(guid);
            if (client == null || client.Player == null || client.Player.Name == null)
                return "{" + guid + "}";
            return client.Player.Name;
        }

        public async Task SendPacketToClient(string guid, Packet packet)
        {
            Client client = GetClient(guid);

            bool shouldQueue = false;
            if (packet is PacketForPlayer packetForPlayer)
            {
                if (packetForPlayer.For != guid)
                    Log.Warn($"Sending packet {packet.GetType().Name} to client that isn't the originally intended target");

                if (packetForPlayer.ShouldQueue)
                    shouldQueue = true;
            }

            if (client != null && client.State == ClientState.Normal && client.Tcp.Connected)
            {
                // Client connected - send packet
                await client.SendPacket(packet);
            }
            else
            {
                // Client not connected
                if (shouldQueue)
                {
                    Log.Message($"Attempt to send packet to player ({guid}) that isn't connected! Packet will be queued for next time they connect.");
                    QueuedPacketStorage.StorePacket(guid, packet);
                }
                else
                {
                    Log.Message($"Attempt to send packet to player ({guid}) that isn't connected! Packet isn't queuable so it will be thrown out.");
                }
            }
        }

        private async void ClientOnAuthenticated(object sender, Client.ClientEventArgs e)
        {
            Log.Message($"{e.Client.Player.Name} connected");
            AuthenticatedClients.Add(e.Client);

            // Send other colonies
            foreach (Client client in AuthenticatedClients)
            {
                if (client.Player.Guid == e.Client.Player.Guid)
                    continue; // Skip self
            }

            // Send queued packets
            foreach (Packet packet in QueuedPacketStorage.GetQueuedPackets(e.Client.Player.Guid, true))
                await e.Client.SendPacket(packet);
        }

        private void ClientOnDisconnected(object sender, Client.ClientEventArgs e)
        {
            if (e.Client.Player != null)
                Log.Message($"{e.Client.Player.Name} disconnected");
            AuthenticatedClients.Remove(e.Client);

            // Update other players on this player (so they can see they're no longer tradeable)
            foreach (Client client in AuthenticatedClients)
            {
                if (e.Client.Player == null)
                    continue; // Null player

                _ = client.SendPacket(new PacketPlayerDisconnected
                {
                    Player = e.Client.Player.Guid,
                    Reason = "Disconnected"
                });
            }
        }

        private void ClientOnColonyInfoReceived(object sender, Client.ClientEventArgs e)
        {
            foreach (Client client in AuthenticatedClients)
            {
                if (client.Player.Guid == e.Client.Player.Guid)
                    continue; // Skip ourselves

                _ = client.SendPacket(new PacketColonyInfo
                {
                    Guid = e.Client.Player.Guid,
                    Player = e.Client.Player
                });
            }
        }
    }
}
