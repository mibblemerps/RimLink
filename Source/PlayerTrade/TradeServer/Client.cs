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
        public event EventHandler<ClientEventArgs> TradableChanged;

        public ClientState State { get; set; } = ClientState.Auth;

        public string Username;

        // [Trade GUID, Target username]
        public Dictionary<Guid, string> TradeOffers = new Dictionary<Guid, string>();

        /// <summary>
        /// Can this client be traded with currently?
        /// </summary>
        public bool IsTradableNow
        {
            get => _isTradableNow;
            set
            {
                _isTradableNow = value;
                TradableChanged?.Invoke(this, new ClientEventArgs(this));
            }
        }

        private bool _isTradableNow;

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

            IsTradableNow = false; // todo: change how this is set
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
                        Username = connectPacket.Username;
                        State = ClientState.Normal;
                        Authenticated?.Invoke(this, new ClientEventArgs(this));

                        IsTradableNow = true; // todo: change how this is set to true

                        break;
                    default:
                        throw new Exception($"Unknown packet received! ID: {e.Id}");
                }
            }
            else
            {
                switch (e.Id)
                {
                    case Packet.ColonyResourcesId:
                        PacketColonyResources resourcePacket = (PacketColonyResources) e.Packet;
                        Log.Message($"Received resource info from {Username}. {resourcePacket.Resources.Things.Count} things");
                        _colonyResourcesCompletionSource?.SetResult(resourcePacket.Resources);
                        break;

                    case Packet.InitiateTradeId:
                        PacketInitiateTrade initiateTradePacket = (PacketInitiateTrade) e.Packet;
                        Log.Message($"{Username} is initiating a trade with {initiateTradePacket.Username}");

                        // Request colony resources from trade partner
                        Client tradePartner = Program.Server.GetClient(initiateTradePacket.Username);
                        Resources resources = await tradePartner.GetColonyResourcesAsync();

                        // Got partner resources - send to initiating client (this client)
                        await SendPacket(new PacketColonyResources(tradePartner.Username, resources));
                        break;

                    case Packet.TradeOfferPacketId:
                        PacketTradeOffer tradeOfferPacket = (PacketTradeOffer) e.Packet;
                        TradeOffers.Add(tradeOfferPacket.Guid, tradeOfferPacket.For);
                        Log.Message($"Received trade offer from {Username} for {tradeOfferPacket.For}");

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
                        foreach (Client client in Program.Server.Clients)
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
                            Log.Warn($"{Username} attempted to accept trade offer that no longer exists.");
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

                        Log.Message($"{Username} {(acceptTradePacket.Accept ? "accepted" : "declined")} trade offer {acceptTradePacket.Trade} from {otherClient.Username}");
                        break;

                    case Packet.ConfirmTradePacketId:
                        PacketTradeConfirm packetTradeConfirm = (PacketTradeConfirm) e.Packet;

                        if (!TradeOffers.ContainsKey(packetTradeConfirm.Trade))
                        {
                            // Trade doesn't exist
                            Log.Warn($"{Username} attempted to confirm trade that no longer exists.");
                            return;
                        }

                        // Get other client we're trading with
                        Client tradeClient = Program.Server.GetClient(TradeOffers[packetTradeConfirm.Trade]);

                        // Forward trade confirm packet
                        await tradeClient.SendPacket(packetTradeConfirm);

                        Log.Message($"{Username} {(packetTradeConfirm.Confirm ? "confirmed" : "aborted")} trade offer {packetTradeConfirm.Trade} for {tradeClient.Username}.");

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
