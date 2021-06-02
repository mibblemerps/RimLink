using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayerTrade;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace TradeServer
{
    public class Server
    {
        public const string ServerSettingsFile = "server-settings.json";

        public List<Client> AuthenticatedClients = new List<Client>();

        public ServerSettings ServerSettings;
        public GameSettings GameSettings => ServerSettings.GameSettings;
        public QueuedPacketStorage QueuedPacketStorage = new QueuedPacketStorage();

        protected TcpListener Listener;

        public Server()
        {
            LoadSettings();
            SaveSettings();
        }

        public async Task Run(int port = 35562)
        {
            QueuedPacketStorage.Load();

            Listener = new TcpListener(IPAddress.IPv6Any, port);
            Listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            Listener.Start();

            while (true)
            {
                try
                {
                    TcpClient tcp = await Listener.AcceptTcpClientAsync();
                    Log.Message($"Accepted TCP connection from {tcp.Client.RemoteEndPoint}");

                    var client = new Client();
                    client.Authenticated += ClientOnAuthenticated;
                    
                    // Serve will setup the connection and perform the handshake. Once that's done, begin the send/receive loops
                    _ = client.Serve(tcp).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            if (t.Exception != null) throw t.Exception;
                            return;
                        }

                        client.Run();
                    });
                }
                catch (Exception e)
                {
                    Log.Error("Exception in accept client loop!", e);
                }
            }
        }

        public Client GetClient(string guid)
        {
            return AuthenticatedClients.FirstOrDefault(client => client.Player.Guid == guid);
        }

        public string GetName(string guid)
        {
            var client = GetClient(guid);
            if (client?.Player?.Name == null)
                return "{" + guid + "}";
            return client.Player.Name;
        }

        public void SendPacketToClient(string guid, Packet packet)
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

            if (client != null && client.State == Connection.ConnectionState.Authenticated && client.Tcp.Connected)
            {
                // Client connected - send packet
                client.SendPacket(packet);
            }
            else
            {
                // Client not connected
                if (shouldQueue)
                {
                    if (!packet.Attribute.HideFromLog)
                        Log.Message($"Attempt to send packet to player ({guid}) that isn't connected! Packet will be queued for next time they connect.");
                    QueuedPacketStorage.StorePacket(guid, packet);
                }
                else
                {
                    if (!packet.Attribute.HideFromLog)
                        Log.Message($"Attempt to send packet to player ({guid}) that isn't connected! Packet isn't queuable so it will be thrown out.");
                }
            }
        }

        public void LoadSettings()
        {
            try
            {
                ServerSettings = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(ServerSettingsFile));
            }
            catch (Exception e)
            {
                Log.Warn($"Couldn't load server settings ({e.Message}). A new server settings file will be used instead.");
                ServerSettings = new ServerSettings();

                // Attempt to backup old settings - if they exist
                try
                {
                    if (File.Exists(ServerSettingsFile))
                        File.Copy(ServerSettingsFile, ServerSettingsFile + ".backup", true);
                }
                catch (Exception) { /* ignored */ }
            }
        }

        public void SaveSettings()
        {
            try
            {
                File.WriteAllText(ServerSettingsFile, JsonConvert.SerializeObject(ServerSettings, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                }));
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't save server settings!", e);
            }
        }

        private async void ClientOnAuthenticated(object sender, Client.ClientEventArgs e)
        {
            Log.Message($"{e.Client.Player.Name} connected");
            AuthenticatedClients.Add(e.Client);

            e.Client.Disconnected += (s, args) => ClientOnDisconnected(this, new Client.ClientEventArgs(e.Client));
            e.Client.ColonyInfoReceived += ClientOnColonyInfoReceived;

            // Send other colonies
            foreach (Client client in AuthenticatedClients)
            {
                if (client.Player.Guid == e.Client.Player.Guid)
                    continue; // Skip self
                e.Client.SendPacket(new PacketColonyInfo
                {
                    Guid = client.Player.Guid,
                    Player = client.Player
                });
            }

            // Send queued packets
            foreach (Packet packet in QueuedPacketStorage.GetQueuedPackets(e.Client.Player.Guid, true))
                e.Client.SendPacket(packet);
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

                client.SendPacket(new PacketPlayerDisconnected
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

                client.SendPacket(new PacketColonyInfo
                {
                    Guid = e.Client.Player.Guid,
                    Player = e.Client.Player
                });
            }
        }
    }
}
