using System;
using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Trade.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.Trade
{
    public class TradeSystem : ISystem
    {
        public Client Client;

        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketRequestColonyResources requestResourcesPacket)
            {
                // Send server our current resources
                Log.Message($"Fulfilling request for colony resources...");
                SendColonyResources();
            }
            else if (e.Packet is PacketTradeOffer tradeOfferPacket)
            {
                TradeOffer newTradeOffer = tradeOfferPacket.ToTradeOffer();
                ActiveTradeOffers.Add(newTradeOffer);
                TradeUtil.PresentTradeOffer(newTradeOffer);
            }
            else if (e.Packet is PacketAcceptTrade acceptTradePacket)
            {
                HandleAcceptTradePacket(acceptTradePacket);
            }
            else if (e.Packet is PacketTradeConfirm confirmTradePacket)
            {
                HandleConfirmTradePacket(confirmTradePacket);
            }
            else if (e.Packet is PacketRetractTrade retractPacket)
            {
                TradeUtil.RetractOffer(ActiveTradeOffers.First(offer => offer.Guid == retractPacket.Guid));
            }
        }

        public void SendOffer(TradeOffer tradeOffer)
        {
            ActiveTradeOffers.Add(tradeOffer);

            Log.Message("Sending trade offer...");
            Client.SendPacket(PacketTradeOffer.MakePacket(tradeOffer));
        }

        public void SendColonyResources()
        {
            try
            {
                var resources = new Resources();
                resources.Update(Find.CurrentMap);
                Client.SendPacket(new PacketColonyResources(Client.Guid, resources));
                Log.Message($"Sent resource info ({resources.Things.Count} things, {resources.Pawns.Count} pawns)");
            }
            catch (Exception e)
            {
                Log.Error("Exception sending colony resources!", e);
                throw;
            }
        }

        private void HandleAcceptTradePacket(PacketAcceptTrade packet)
        {
            bool confirm = packet.Accept;

            TradeOffer acceptOffer = ActiveTradeOffers.FirstOrDefault(tradeOffer => tradeOffer.Guid == packet.Trade);
            if (acceptOffer == null)
            {
                // Offer not found (probably expired) - send rejection
                Client.SendPacket(new PacketTradeConfirm
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
                Client.SendPacket(new PacketTradeConfirm
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
                Find.LetterStack.ReceiveLetter($"Trade Rejected ({RimLinkComp.Find().Client.GetName(acceptOffer.For)})", $"{acceptOffer.For} rejected your trade offer.", LetterDefOf.NeutralEvent);
            }

            acceptOffer.TradeAccepted?.TrySetResult(confirm);

            // Remove trade offer
            ActiveTradeOffers.Remove(acceptOffer);
        }

        private void HandleConfirmTradePacket(PacketTradeConfirm packet)
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
                Find.LetterStack.ReceiveLetter($"Trade Failed ({RimLinkComp.Instance.Client.GetName(offer.From)})", $"Trade from {RimLinkComp.Instance.Client.GetName(offer.From)} is no longer available.", LetterDefOf.NeutralEvent);
            }

            offer.TradeAccepted?.TrySetResult(confirm);

            ActiveTradeOffers.Remove(offer);
        }

        public void Update()
        {
            
        }
        
        public void ExposeData()
        {
            
        }
    }
}
