﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Labor
{
    public class LaborWorker
    {
        public Client Client;
        public List<LaborOffer> Offers = new List<LaborOffer>();

        public LaborWorker(Client client)
        {
            Client = client;

            Client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            switch (e.Id)
            {
                case Packet.LaborOfferPacketId:
                    LaborOffer offer = LaborOffer.FromPacket((PacketLaborOffer) e.Packet);
                    Log.Message($"Received labor offer {offer.Guid} from {Client.GetName(offer.From)}");
                    LaborUtil.PresentLendColonistOffer(offer);
                    break;

                case Packet.AcceptLaborOfferPacketId:
                    HandleAcceptOfferPacket((PacketAcceptLaborOffer) e.Packet);
                    break;

                case Packet.ConfirmLaborOfferPacketId:
                    PacketConfirmLaborOffer confirmPacket = (PacketConfirmLaborOffer) e.Packet;
                    Log.Message("Received labor offer confirmation: " + confirmPacket.Guid);
                    break;
            }
        }

        private async void HandleAcceptOfferPacket(PacketAcceptLaborOffer packet)
        {
            LaborOffer offer = Offers.FirstOrDefault(o => o.Guid == packet.Guid);
            if (offer == null)
            {
                Log.Warn($"Player accepted to accept a non-existent offer ({packet.Guid}).");
                return;
            }

            bool fulfill = offer.LenderCanStillFulfill();

            Log.Message($"Received acceptance of labor offer {offer.Guid}. Fulfill = {fulfill}");

            await Client.SendPacket(new PacketConfirmLaborOffer
            {
                For = offer.For,
                Guid = offer.Guid,
                Confirm = fulfill
            });

            if (fulfill)
            {
                offer.FulfillAsSender(Find.CurrentMap);
                Find.LetterStack.ReceiveLetter($"Labor Offer Accepted ({RimLinkComp.Find().Client.GetName(offer.For)})", $"Your offer to lend colonists has been accepted.\n\n" +
                    $"Your {(offer.Colonists.Count == 1 ? "colonist" : "colonists")} should (hopefully) be returned in {(offer.Days).ToString().Colorize(ColoredText.DateTimeColor)} days (other colonies time).", LetterDefOf.PositiveEvent);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Labor Offer Failed ({RimLinkComp.Find().Client.GetName(offer.For)})", "The pawns offered are not in the same condition as when they were initially offered.", LetterDefOf.NegativeEvent);
            }
        }
    }
}
