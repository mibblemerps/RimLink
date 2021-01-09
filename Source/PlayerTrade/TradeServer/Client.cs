using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Net;

namespace TradeServer
{
    public class Client : Connection
    {
        public event EventHandler<ClientEventArgs> Authenticated;
        public event EventHandler<ClientEventArgs> Disconnected;
        public event EventHandler<ClientEventArgs> ColonyInfoReceived;

        public ClientState State { get; set; } = ClientState.Auth;

        public Player Player;
        //public string Username;

        // [Trade GUID, Target username]
        public Dictionary<Guid, string> TradeOffers = new Dictionary<Guid, string>();

        private TaskCompletionSource<Resources> _colonyResourcesCompletionSource;

        public Client(TcpClient connection)
        {
            Tcp = connection;
            Stream = connection.GetStream();

            PacketReceived += OnPacketReceived;
        }

        public async Task Run()
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

            Disconnected?.Invoke(this, new ClientEventArgs(this));
        }

        public async Task<Resources> GetColonyResourcesAsync()
        {
            _colonyResourcesCompletionSource = new TaskCompletionSource<Resources>();

            // Request colony resources
            await SendPacket(new PacketRequestColonyResources());

            // Await response
            Resources resources = await _colonyResourcesCompletionSource.Task;

            _colonyResourcesCompletionSource = null; // Null completion source since it's been used

            return resources;
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            Log.Message($"Packet received {e.Id} from {Tcp.Client.RemoteEndPoint}");

            if (State == ClientState.Auth)
            {
                switch (e.Id)
                {
                    case Packet.ConnectId:
                        PacketConnect connectPacket = (PacketConnect) e.Packet;
                        Player = connectPacket.Player;

                        State = ClientState.Normal;
                        Authenticated?.Invoke(this, new ClientEventArgs(this));
                        // todo: validate player secret

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
                    Client target = Program.Server.GetClient(forPlayer.For);
                    if (target == null)
                    {
                        Log.Warn($"Attempt to route packet {e.Packet.GetType().Name} to player that doesn't exist ({forPlayer.For}).");
                        return;
                    }

                    // Forward packet
                    await target.SendPacket(forPlayer);
                    Log.Message($"Packet ID {e.Id} forwarded from {Player.Name} -> {target.Player.Name}");
                }

                switch (e.Id)
                {
                    case Packet.ColonyInfoId:
                        PacketColonyInfo colonyInfoPacket = (PacketColonyInfo) e.Packet;
                        Player = colonyInfoPacket.Player;
                        Log.Message($"Received colony info from {Player.Name} (tradeable = {Player.TradeableNow})");
                        ColonyInfoReceived?.Invoke(this, new ClientEventArgs(this));
                        break;

                    case Packet.ColonyResourcesId:
                        PacketColonyResources resourcePacket = (PacketColonyResources) e.Packet;
                        Log.Message($"Received resource info from {Player.Name}. {resourcePacket.Resources.Things.Count} things");
                        _colonyResourcesCompletionSource?.SetResult(resourcePacket.Resources);
                        break;

                    case Packet.InitiateTradeId:
                        PacketInitiateTrade initiateTradePacket = (PacketInitiateTrade) e.Packet;
                        Log.Message($"{Player.Name} is initiating a trade with {Program.Server.GetName(initiateTradePacket.Guid)}");

                        // Request colony resources from trade partner
                        Client tradePartner = Program.Server.GetClient(initiateTradePacket.Guid);
                        Resources resources = await tradePartner.GetColonyResourcesAsync();

                        // Got partner resources - send to initiating client (this client)
                        await SendPacket(new PacketColonyResources(tradePartner.Player.Guid, resources));
                        break;

                    case Packet.TradeOfferPacketId:
                        PacketTradeOffer tradeOfferPacket = (PacketTradeOffer) e.Packet;
                        TradeOffers.Add(tradeOfferPacket.Guid, tradeOfferPacket.For);
                        Log.Message($"Received trade offer from {Player.Name} for {Program.Server.GetName(tradeOfferPacket.For)}");

                        try
                        {
                            // Pass trade offer on to other player. Packet can be relayed verbatim
                            await Program.Server.GetClient(tradeOfferPacket.For).SendPacket(tradeOfferPacket);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error passing on trade offer", ex);
                        }
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
                            await SendPacket(new PacketTradeConfirm
                            {
                                Trade = acceptTradePacket.Trade,
                                Confirm = false
                            });
                            return;
                        }

                        // Send packet acceptance to client who made trade offer
                        await otherClient.SendPacket(new PacketAcceptTrade
                        {
                            Trade = acceptTradePacket.Trade,
                            Accept = acceptTradePacket.Accept
                        });

                        // Now we just have to wait for the client who made the offer to send a trade confirmation packet

                        Log.Message($"{Player.Name} {(acceptTradePacket.Accept ? "accepted" : "declined")} trade offer {acceptTradePacket.Trade} from {otherClient.Player.Name}");
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
                        await tradeClient.SendPacket(packetTradeConfirm);

                        Log.Message($"{Player.Name} {(packetTradeConfirm.Confirm ? "confirmed" : "aborted")} trade offer {packetTradeConfirm.Trade} for {tradeClient.Player.Name}.");

                        break;

                    case Packet.TriggerRaidPacketId:
                        PacketTriggerRaid triggerRaidPacket = (PacketTriggerRaid) e.Packet;
                        Client target = Program.Server.GetClient(triggerRaidPacket.For);
                        if (target == null)
                        {
                            Log.Warn($"Player ({Player.Name}) tried to sent raid to an unknown client ({triggerRaidPacket.For}).");
                            return;
                        }

                        Log.Message($"Player {Player.Name} has sent a raid to {target.Player.Name}");

                        // Forward packet
                        await target.SendPacket(triggerRaidPacket);
                        break;

                    case Packet.RaidAcceptedPacketId:
                        PacketRaidAccepted acceptRaidPacket = (PacketRaidAccepted) e.Packet;
                        Log.Message($"Player {Player.Name} acknowledged raid {acceptRaidPacket.Id}");
                        // Pass on packet
                        Program.Server.GetClient(acceptRaidPacket.For)?.SendPacket(acceptRaidPacket);
                        break;
                }
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
