using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class Client : Connection
    {
        public List<string> TradablePlayers = new List<string>();

        public string Username;

        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();

        private TaskCompletionSource<PacketColonyResources> _awaitColonyResources = new TaskCompletionSource<PacketColonyResources>();

        public bool IsTradableNow
        {
            get => _isTradableNow;
            set
            {
                _isTradableNow = value;
                _ = PlayerTradeMod.Instance.Client.SendPacket(new PacketColonyTradable()
                {
                    Username = Username,
                    TradableNow = value
                });
            }
        }

        private bool _isTradableNow;

        public async Task Connect(string username, string ip, int port = 35562)
        {
            Username = username;

            Tcp = new TcpClient();
            await Tcp.ConnectAsync(ip, port);
            Stream = Tcp.GetStream();

            PacketReceived += OnPacketReceived;

            await SendPacket(new PacketConnect()
            {
                ProtocolVersion = 1,
                Username = username
            });

            Run();
        }

        public async Task Run()
        {
            while (Tcp.Connected)
            {
                try
                {
                    await ReceivePacket();
                }
                catch (Exception e)
                {
                    Log.Error("Error receiving packet", e);
                }
            }

            Log.Message($"Disconnected from Trade server");
        }

        /// <summary>
        /// Send the current trade deal
        /// </summary>
        /// <returns></returns>
        public async Task SendTradeOffer()
        {
            TradeOffer tradeOffer = TradeUtil.FormTradeOffer();
            ActiveTradeOffers.Add(tradeOffer);

            Log.Message("Forming trade packet...");
            PacketTradeOffer tradeReqPacket = null;
            try
            {
                tradeReqPacket = PacketTradeOffer.MakePacket(tradeOffer);
            }
            catch (Exception e)
            {
                Log.Error("Exception making trade offer packet", e);
                return;
            }

            Log.Message("Sending trade offer...");
            await SendPacket(tradeReqPacket);
            Log.Message("Trade offer sent");
        }

        public async Task<PacketColonyResources> GetColonyResources(string username)
        {
            // Send trade request packet
            await SendPacket(new PacketInitiateTrade
            {
                Username = username
            });

            // Await response
            PacketColonyResources packet = await _awaitColonyResources.Task;
            _awaitColonyResources = new TaskCompletionSource<PacketColonyResources>(); // reset task completion source for future requests
            return packet;
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            switch (e.Id)
            {
                case Packet.ColonyTradableId:
                    PacketColonyTradable tradablePacket = (PacketColonyTradable) e.Packet;
                    if (tradablePacket.TradableNow)
                    {
                        if (TradablePlayers.Contains(tradablePacket.Username))
                            return; // Player already listed as tradable - don't add duplicate
                        TradablePlayers.Add(tradablePacket.Username);
                        Log.Message($"{tradablePacket.Username} is now tradable");
                        Messages.Message($"{tradablePacket.Username} is now tradable", def: MessageTypeDefOf.NeutralEvent, historical: false);
                        
                    }
                    else
                    {
                        TradablePlayers.Remove(tradablePacket.Username);
                        Log.Message($"{tradablePacket.Username} no longer tradable");
                        Messages.Message($"{tradablePacket.Username} is no longer tradable", def: MessageTypeDefOf.NeutralEvent, historical: false);
                    }
                    break;

                case Packet.ColonyResourcesId:
                    PacketColonyResources resourcesPacket = (PacketColonyResources) e.Packet;
                    _awaitColonyResources.TrySetResult(resourcesPacket);
                    break;

                case Packet.RequestColonyResourcesId:
                    // Send server our current resources
                    Log.Message($"Fulfilling request for colony resources...");
                    await SendColonyResources();
                    break;

                case Packet.TradeOfferPacketId:
                    TradeOffer newTradeOffer = ((PacketTradeOffer) e.Packet).ToTradeOffer();
                    ActiveTradeOffers.Add(newTradeOffer);
                    TradeUtil.PresentTradeOffer(newTradeOffer);
                    break;

                case Packet.AcceptTradePacketId:
                    await HandleAcceptTradePacket((PacketAcceptTrade) e.Packet);
                    break;

                case Packet.ConfirmTradePacketId:
                    await HandleConfirmTradePacket((PacketTradeConfirm) e.Packet);
                    break;
            }
        }

        public async Task SendColonyResources()
        {
            var resources = new Resources();
            resources.Update(Find.CurrentMap);
            await SendPacket(new PacketColonyResources(Username, resources));
        }

        private async Task HandleAcceptTradePacket(PacketAcceptTrade packet)
        {
            bool confirm = packet.Accept;

            TradeOffer acceptOffer = ActiveTradeOffers.FirstOrDefault(tradeOffer => tradeOffer.Guid == packet.Trade);
            if (acceptOffer == null)
            {
                // Offer not found (probably expired) - send rejection
                await SendPacket(new PacketTradeConfirm
                {
                    Trade = packet.Trade,
                    Confirm = false
                });
                return;
            }

            if (acceptOffer.IsForUs)
            {
                // Trade not for us (it'd be a trade we've sent)
                Log.Warn("Received a offer acceptance for a trade we received. This isn't right, we're supposed to be the one sending the acceptance!");
                confirm = false;
            }

            if (!acceptOffer.CanFulfill(false))
            {
                // We cannot fulfill the trade
                Messages.Message($"Unable to fulfill trade for {acceptOffer.For} - missing resources.", MessageTypeDefOf.NeutralEvent, false);
                confirm = false;
            }

            if (packet.Accept) // don't send confirmation/rejection if the other party declined the trade, no point confirming a rejected trade
            {
                // Send confirmation/rejection
                await SendPacket(new PacketTradeConfirm
                {
                    Trade = packet.Trade,
                    Confirm = confirm
                });
            }

            if (confirm)
            {
                acceptOffer.Fulfill(false);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Trade Rejected ({acceptOffer.For})", $"{acceptOffer.For} rejected your trade offer.", LetterDefOf.NeutralEvent);
            }

            acceptOffer.TradeAccepted?.TrySetResult(confirm);

            // Remove trade offer
            ActiveTradeOffers.Remove(acceptOffer);
        }

        private async Task HandleConfirmTradePacket(PacketTradeConfirm packet)
        {
            TradeOffer offer = ActiveTradeOffers.FirstOrDefault(tradeOffer => tradeOffer.Guid == packet.Trade);

            bool confirm = packet.Confirm;

            if (offer == null)
            {
                // Offer not found (probably expired).
                return;
            }

            if (!offer.IsForUs)
            {
                // Trade not for us (it'd be a trade we've sent)
                Log.Warn("Received a offer confirmation for a trade we sent! We're supposed to be the ones sending the trade confirmation!");
                confirm = false;
            }

            if (confirm)
            {
                offer.Fulfill(true);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Trade Failed ({offer.From})", $"Trade from {offer.From} is no longer available.", LetterDefOf.NeutralEvent);
            }

            offer.TradeAccepted?.TrySetResult(confirm);

            ActiveTradeOffers.Remove(offer);
        }
    }
}
