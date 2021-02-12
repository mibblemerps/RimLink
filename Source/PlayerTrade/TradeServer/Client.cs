using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Chat;
using PlayerTrade.Net;
using TradeServer.Commands;

namespace TradeServer
{
    public class Client : Connection
    {
        public event EventHandler<ClientEventArgs> Authenticated;
        public event EventHandler<ClientEventArgs> ColonyInfoReceived;

        public ClientState State { get; set; } = ClientState.Auth;

        public Player Player;
        public PlayerInfo PlayerInfo;

        public Caller CommandCaller;

        // [Trade GUID, Target username]
        public Dictionary<Guid, string> TradeOffers = new Dictionary<Guid, string>();

        private TaskCompletionSource<Resources> _colonyResourcesCompletionSource;

        public Client(TcpClient connection)
        {
            Tcp = connection;
            Stream = connection.GetStream();

            PacketReceived += OnPacketReceived;

            CommandCaller = new ClientCaller(this);
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
                    if (await ReceivePacket() == null)
                        break;
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
            while (true)
            {
                await SendQueuedPackets();
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
                if (State == ClientState.Auth)
                {
                    switch (e.Id)
                    {
                        case Packet.ConnectId:
                            await HandleConnectPacket((PacketConnect) e.Packet);
                            break;

                        case Packet.PingPacketId:
                            await HandlePingPacket();
                            break;
                        default:
                            throw new Exception($"Unknown packet received! ID: {e.Id}");
                    }
                }
                else
                {
                    /*
                     * Sending colonist only works once, then it breaks until server is restarted.
                     * Possibly related to this code I dunno??
                     */
                    if (e.Packet is PacketForPlayer forPlayer)
                    {
                        if (forPlayer.For == Player.Guid)
                        {
                            Log.Warn("Attempt to route packet to same client it was sent from. This isn't permitted.");
                            return;
                        }

                        // Forward packet
                        await Program.Server.SendPacketToClient(forPlayer.For, e.Packet);
                    }

                    switch (e.Id)
                    {
                        case Packet.ColonyInfoId:
                            PacketColonyInfo colonyInfoPacket = (PacketColonyInfo) e.Packet;
                            Player = colonyInfoPacket.Player;
                            ColonyInfoReceived?.Invoke(this, new ClientEventArgs(this));
                            break;

                        case Packet.ColonyResourcesId:
                            PacketColonyResources resourcePacket = (PacketColonyResources) e.Packet;
                            Log.Message(
                                $"Received resource info from {Player.Name}. {resourcePacket.Resources.Things.Count} things");
                            _colonyResourcesCompletionSource?.SetResult(resourcePacket.Resources);
                            break;

                        case Packet.InitiateTradeId:
                            PacketInitiateTrade initiateTradePacket = (PacketInitiateTrade) e.Packet;
                            Log.Message(
                                $"{Player.Name} is initiating a trade with {Program.Server.GetName(initiateTradePacket.Guid)}");

                            // Request colony resources from trade partner
                            Client tradePartner = Program.Server.GetClient(initiateTradePacket.Guid);
                            Resources resources = await tradePartner.GetColonyResourcesAsync();

                            // Got partner resources - send to initiating client (this client)
                            SendPacket(new PacketColonyResources(tradePartner.Player.Guid, resources));
                            break;

                        case Packet.TradeOfferPacketId:
                            PacketTradeOffer tradeOfferPacket = (PacketTradeOffer) e.Packet;
                            TradeOffers.Add(tradeOfferPacket.Guid, tradeOfferPacket.For);
                            Log.Message(
                                $"Received trade offer from {Player.Name} for {Program.Server.GetName(tradeOfferPacket.For)}");
                            // Packet will already have been forwarded since it's a PacketForPlayer
                            break;

                        case Packet.AcceptTradePacketId:
                            PacketAcceptTrade acceptTradePacket = (PacketAcceptTrade) e.Packet;

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

                        case Packet.ConfirmTradePacketId:
                            PacketTradeConfirm packetTradeConfirm = (PacketTradeConfirm) e.Packet;

                            if (!TradeOffers.ContainsKey(packetTradeConfirm.Trade))
                            {
                                // Trade doesn't exist
                                Log.Warn($"{Player.Name} attempted to confirm trade that no longer exists.");
                                return;
                            }

                            // Get other client we're trading with
                            Client tradeClient = Program.Server.GetClient(TradeOffers[packetTradeConfirm.Trade]);

                            // Forward trade confirm packet
                            tradeClient.SendPacket(packetTradeConfirm);

                            Log.Message(
                                $"{Player.Name} {(packetTradeConfirm.Confirm ? "confirmed" : "aborted")} trade offer {packetTradeConfirm.Trade} for {tradeClient.Player.Name}.");

                            break;

                        case Packet.SendChatMessagePacketId:
                            var sendMsgPacket = (PacketSendChatMessage) e.Packet;

                            if (sendMsgPacket.Message.StartsWith("/"))
                            {
                                // Command
                                Log.Message($"{Player.Name} executing: " + sendMsgPacket.Message);
                                CommandUtility.ExecuteCommand(CommandCaller, sendMsgPacket.Message);
                            }
                            else
                            {
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
                            }

                            break;
                            
                        case Packet.BugReportPacketId:
                            Log.Message($"Received bug report from {Player.Name} ({Player.Guid})!");
                            BugReportFiler.FileReport(Player, (PacketBugReport) e.Packet);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Player != null)
                    Log.Error($"Error handling packet from {Player.Name} ({Player.Guid})! Connection terminated.", ex);
                Tcp.Close();
            }
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
                Tcp.Close();
                return;
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
                Tcp.Close();
                return;
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
                Tcp.Close();
                return;
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
                Tcp.Close();
                return;
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

            State = ClientState.Normal;
            Authenticated?.Invoke(this, new ClientEventArgs(this));
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
