using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Chat;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Trade.Packets;
using TradeServer.Commands;

namespace TradeServer
{
    public class Client : Connection
    {
        public event EventHandler<ClientEventArgs> Authenticated;
        public event EventHandler<ClientEventArgs> ColonyInfoReceived;

        public float LastHeartbeat;

        public Player Player;
        public PlayerInfo PlayerInfo;

        public Caller CommandCaller;

        // [Trade GUID, Target username]
        public Dictionary<Guid, string> TradeOffers = new Dictionary<Guid, string>();

        private TaskCompletionSource<Resources> _colonyResourcesCompletionSource;

        public Client()
        {
            PacketReceived += OnPacketReceived;
            Authenticated += OnAuthenticated;

            CommandCaller = new ClientCaller(this);
        }

        public override async Task Handshake()
        {
            while (true)
            {
                Packet packet = await ReceivePacket();
                if (packet == null) throw new Exception("Client disconnected");
                
                if (packet is PacketConnect connect)
                {
                    await HandleConnectPacket(connect);
                    break;
                }
                
                if (packet is PacketPing)
                    await HandlePingPacket();
            }

            // Player now authenticated
            Authenticated?.Invoke(this, new ClientEventArgs(this));
        }

        public void Run()
        {
            _ = SendPackets();
            _ = ReceivePackets();
        }

        public async Task ReceivePackets()
        {
            try
            {
                while (Tcp.Connected)
                {
                    await ReceivePacket();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Exception with client {Tcp.Client.RemoteEndPoint}. Connection closed.", e);
                Tcp.Close();
            }
        }

        public async Task SendPackets()
        {
            while (Tcp.Connected)
            {
                await SendQueuedPackets();
            }
        }

        public async Task Heartbeat()
        {
            while (IsConnected)
            {
                if (LastHeartbeat > 0f && Program.Stopwatch.Elapsed.TotalSeconds - LastHeartbeat > PlayerTrade.Net.Client.TimeoutThresholdSeconds)
                {
                    // Timed out
                    Log.Message($"Connection with {this} timed out.");
                    Disconnect(DisconnectReason.Network);
                }

                SendPacket(new PacketHeartbeat());
                await Task.Delay(2000);
            }
        }

        public async Task<Resources> GetColonyResourcesAsync()
        {
            _colonyResourcesCompletionSource = new TaskCompletionSource<Resources>();

            // Request colony resources
            SendPacket(new PacketRequestColonyResources());

            // Await response
            Resources resources = await _colonyResourcesCompletionSource.Task;

            _colonyResourcesCompletionSource = null; // Null completion source since it's been used

            return resources;
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (State == ConnectionState.Connected)
                {
                    // Connected but not authenticated.
                    switch (e.Packet)
                    {
                        case PacketConnect connectPacket:
                            await HandleConnectPacket(connectPacket);
                            break;
                        case PacketPing _:
                            await HandlePingPacket();
                            break;
                        default:
                            throw new Exception($"Cannot handle packet now! {e.Packet.GetType().Name}");
                    }
                }
                else if (State == ConnectionState.Authenticated)
                {
                    // Connected and authenticated as a player.
                    if (e.Packet is PacketForPlayer forPlayer)
                    {
                        if (forPlayer.For == Player.Guid)
                        {
                            Log.Warn("Attempt to route packet to same client it was sent from. This isn't permitted.");
                            return;
                        }
                        if (forPlayer.For == null)
                        {
                            Log.Error($"PacketForPlayer has no target player set. (for == null)");
                            Disconnect(DisconnectReason.Error);
                            return;
                        }

                        if (Program.Server.ServerSettings.LogPacketTraffic && !e.Packet.Attribute.HideFromLog)
                            Log.Message($"[Packet] {e.Packet.GetType().Name} {this} -> {forPlayer.For}");

                        // Forward packet
                        Program.Server.SendPacketToClient(forPlayer.For, e.Packet);
                    }
                    else
                    {
                        if (Program.Server.ServerSettings.LogPacketTraffic && !e.Packet.Attribute.HideFromLog)
                            Log.Message($"[Packet] {e.Packet.GetType().Name} {this}");
                    }

                    switch (e.Packet)
                    {
                        case PacketHeartbeat _:
                            LastHeartbeat = (float)Program.Stopwatch.Elapsed.TotalSeconds;
                            break;
                        
                        case PacketDisconnect _:
                            Disconnect(DisconnectReason.User, "User disconnect");
                            break;

                        case PacketColonyInfo colonyInfoPacket:
                            Player = colonyInfoPacket.Player;
                            ColonyInfoReceived?.Invoke(this, new ClientEventArgs(this));
                            break;

                        case PacketColonyResources resourcesPacket:
                            Log.Message($"Received resource info from {Player.Name}. {resourcesPacket.Resources.Things.Count} things");
                            _colonyResourcesCompletionSource?.SetResult(resourcesPacket.Resources);
                            break;

                        case PacketInitiateTrade initiateTradePacket:
                            Log.Message($"{Player.Name} is initiating a trade with {Program.Server.GetName(initiateTradePacket.Guid)}");

                            // Request colony resources from trade partner
                            Client tradePartner = Program.Server.GetClient(initiateTradePacket.Guid);
                            Resources resources = await tradePartner.GetColonyResourcesAsync();

                            // Got partner resources - send to initiating client (this client)
                            SendPacket(new PacketColonyResources(tradePartner.Player.Guid, resources));
                            break;

                        case PacketTradeOffer tradeOfferPacket:
                            TradeOffers.Add(tradeOfferPacket.Guid, tradeOfferPacket.For);
                            Log.Message($"Received trade offer from {Player.Name} for {Program.Server.GetName(tradeOfferPacket.For)}");
                            // Packet will already have been forwarded since it's a PacketForPlayer
                            break;

                        case PacketAcceptTrade acceptTradePacket:
                            // Find client who made the trade offer
                            Client otherClient = null;
                            foreach (Client client in Program.Server.AuthenticatedClients)
                            {
                                if (client.TradeOffers.ContainsKey(acceptTradePacket.Trade))
                                {
                                    otherClient = client;
                                    break;
                                }
                            }

                            if (otherClient == null)
                            {
                                // Can't find client who made trade offer - send non-confirmation packet
                                Log.Warn($"{Player.Name} attempted to accept trade offer that no longer exists.");
                                SendPacket(new PacketTradeConfirm
                                {
                                    Trade = acceptTradePacket.Trade,
                                    Confirm = false
                                });
                                return;
                            }

                            // Send packet acceptance to client who made trade offer
                            otherClient.SendPacket(new PacketAcceptTrade
                            {
                                Trade = acceptTradePacket.Trade,
                                Accept = acceptTradePacket.Accept
                            });

                            // Now we just have to wait for the client who made the offer to send a trade confirmation packet

                            Log.Message(
                                $"{Player.Name} {(acceptTradePacket.Accept ? "accepted" : "declined")} trade offer {acceptTradePacket.Trade} from {otherClient.Player.Name}");
                            break;

                        case PacketTradeConfirm confirmTradePacket:
                            if (!TradeOffers.ContainsKey(confirmTradePacket.Trade))
                            {
                                Log.Warn($"{Player.Name} attempted to confirm trade that no longer exists.");
                                return;
                            }

                            Client tradeClient = Program.Server.GetClient(TradeOffers[confirmTradePacket.Trade]);

                            // Forward trade confirm packet
                            tradeClient.SendPacket(confirmTradePacket);

                            Log.Message($"{Player.Name} {(confirmTradePacket.Confirm ? "confirmed" : "aborted")} trade offer {confirmTradePacket.Trade} for {tradeClient.Player.Name}.");
                            break;
                        
                        case PacketSendChatMessage sendMsgPacket when sendMsgPacket.Message.StartsWith("/"):
                            // Command
                            Log.Message($"{this} executing: " + sendMsgPacket.Message);
                            CommandUtility.ExecuteCommand(CommandCaller, sendMsgPacket.Message);
                            break;

                        case PacketSendChatMessage sendMsgPacket:
                            // Chat message
                            Log.Message($"[Chat] <{Player.Name}> {sendMsgPacket.Message}");
                            // Send message to all other clients
                            foreach (Client client in Program.Server.AuthenticatedClients)
                                client.SendPacket(new PacketReceiveChatMessage
                                {
                                    Messages = new List<PacketReceiveChatMessage.NetMessage>(new[]
                                    {
                                        new PacketReceiveChatMessage.NetMessage
                                        {
                                            From = Player.Guid,
                                            Message = sendMsgPacket.Message
                                        }
                                    })
                                });
                            break;
                        
                        case PacketBugReport bugReportPacket:
                            Log.Message($"Received bug report from {this}!");
                            BugReportFiler.FileReport(Player, bugReportPacket);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling packet from {this}! Connection terminated.", ex);
                Disconnect(DisconnectReason.Error);
            }
        }

        private void OnAuthenticated(object sender, ClientEventArgs e)
        {
            // Start heatbeating
            LastHeartbeat = (float) Program.Stopwatch.Elapsed.TotalSeconds;
            _ = Heartbeat();
        }

        private async Task HandleConnectPacket(PacketConnect packet)
        {
            Player = packet.Player;

            // Check protocol version
            if (packet.ProtocolVersion != RimLinkMod.ProtocolVersion)
            {
                // Mismatch!
                Log.Warn($"Player {packet.Guid} attempted to connect with an incorrect version of RimLink!");
                string failReason = packet.ProtocolVersion < RimLinkMod.ProtocolVersion 
                    ? "You are running an out-of-date version of RimLink!"
                    : "The server is running an out-of-date version of RimLink!";
                await SendPacketDirect(new PacketConnectResponse
                {
                    Success = false,
                    FailReason = failReason,
                    AllowReconnect = false
                });
                throw new ConnectionFailedException("Player attempted to join with an incorrect version of RimLink.");
            }

            // Check this GUID isn't already connected
            Client conflictClient = Program.Server.GetClient(packet.Guid);
            if (conflictClient != null)
            {
                // Already logged in - reject connection
                Log.Warn($"Player {packet.Player.Name} ({packet.Guid}) attempted to connect whilst being connected from another location.");
                await SendPacketDirect(new PacketConnectResponse
                {
                    Success = false,
                    FailReason = "This game is already connected to the server.",
                    AllowReconnect = false
                });
                throw new ConnectionFailedException("Game already connected to the server.");
            }

            // Load player info
            PlayerInfo = PlayerInfo.Load(Player.Guid, true);
            PlayerInfo.LastOnline = DateTime.Now;
            PlayerInfo.Save();

            // Check secret
            if (PlayerInfo.Secret != null && !PlayerInfo.Secret.Equals(packet.Secret, StringComparison.InvariantCulture))
            {
                // Invalid secret
                Log.Warn($"Player {packet.Player.Name} ({packet.Guid}) attempted to connect with incorrect secret.");
                await SendPacketDirect(new PacketConnectResponse
                {
                    Success = false,
                    FailReason = "Game secret incorrect. RimLink data may be corrupted.",
                    AllowReconnect = false
                });
                throw new ConnectionFailedException("Game secret incorrect.");
            }

            // Assign secret
            if (PlayerInfo.Secret == null)
            {
                PlayerInfo.Secret = packet.Secret;
                PlayerInfo.Save();
            }

            // Check if banned
            if (PlayerInfo.IsBanned)
            {
                // Banned
                Log.Warn($"Banned player {packet.Player.Name} tried to join.");
                string banExpiryMessage = "";
                if (PlayerInfo.BannedUntil.HasValue && PlayerInfo.BannedUntil.Value < DateTime.MaxValue)
                {
                    banExpiryMessage = "Your ban will expire in " +
                                       (PlayerInfo.BannedUntil.Value - DateTime.Now).ToHumanString() + "\n";
                }

                await SendPacketDirect(new PacketConnectResponse
                {
                    Success = false,
                    FailReason = "Banned from server.\n" + banExpiryMessage +
                                 (PlayerInfo.BanReason == null ? "" : $"\n{PlayerInfo.BanReason}"),
                    AllowReconnect = false
                });
                throw new ConnectionFailedException($"Banned from the server ({banExpiryMessage})");
            }

            // Send connect response with connected player data
            var players = new List<Player>();
            foreach (var client in Program.Server.AuthenticatedClients.Where(c => c.Player != null))
                players.Add(client.Player);

            SendPacket(new PacketConnectResponse
            {
                Success = true,
                ConnectedPlayers = players,
                Settings = Program.Server.GameSettings
            });
        }

        private async Task HandlePingPacket()
        {
            var players = new List<Player>();
            foreach (Client client in Program.Server.AuthenticatedClients)
                players.Add(client.Player);

            await SendPacketDirect(new PacketPingResponse
            {
                ServerName = Program.Server.ServerSettings.GameSettings.ServerName,
                ProtocolVersion = RimLinkMod.ProtocolVersion,
                MaxPlayers = Program.Server.ServerSettings.MaxPlayers,
                PlayersOnline = players
            });
        }

        public override string ToString()
        {
            if (Player == null)
            {
                return Tcp?.Client != null ? $"{Tcp.Client.RemoteEndPoint}" : "Unknown Client " + base.ToString();
            }
            else
            {
                return $"{Player.Name} ({Player.Guid})";
            }
        }

        public class ClientEventArgs : EventArgs
        {
            public readonly Client Client;

            public ClientEventArgs(Client client)
            {
                Client = client;
            }
        }
    }
}
