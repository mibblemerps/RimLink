using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Util;
using RimLink.Core;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Systems.Trade.Packets;
using RimWorld;
using Verse;

namespace RimLink.Systems.Trade
{
    public class TradeSystem : ISystem
    {
        public Client Client;

        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
            client.PlayerUpdated += OnPlayerUpdated;
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
        
        private void OnPlayerUpdated(object sender, Client.PlayerUpdateEventArgs e)
        {
            if (e.OldPlayer == null) return; // Don't issue update for newly joined players
            
            // If tradeable status changed
            if (e.Player.TradeableNow != e.OldPlayer.TradeableNow)
            {
                Messages.Message((e.Player.TradeableNow ? "Rl_MessagePlayerIsNowTradeable" : "Rl_MessagePlayerIsNoLongerTradeable")
                        .Translate(e.Player.Name.Colorize(e.Player.Color.ToColor())),
                    def: MessageTypeDefOf.NeutralEvent, false);
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
                Messages.Message("Rl_MissingResources".Translate(acceptOffer.For.GuidToName(true)), MessageTypeDefOf.NeutralEvent, false);
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
                Find.LetterStack.ReceiveLetter("Rl_TradeRejected".Translate(acceptOffer.For.GuidToName()),
                    "Rl_TradeRejectedDesc".Translate(acceptOffer.For.GuidToName(true)), LetterDefOf.NeutralEvent);
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
                Find.LetterStack.ReceiveLetter("Rl_TradeFailed".Translate(offer.From.GuidToName()),
                    "Rl_TradeFailedDesc".Translate(offer.From.GuidToName(true)), LetterDefOf.NeutralEvent);
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
