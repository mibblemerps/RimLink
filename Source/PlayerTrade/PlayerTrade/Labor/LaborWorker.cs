using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Labor.Packets;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
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
            if (e.Packet is PacketLaborOffer offerPacket)
            {
                LaborOffer offer = LaborOffer.FromPacket(offerPacket);
                Log.Message($"Received labor offer {offer.Guid} from {Client.GetName(offer.From)}");
                LaborUtil.PresentLendColonistOffer(offer);
            }
            else if (e.Packet is PacketAcceptLaborOffer acceptPacket)
            {
                HandleAcceptOfferPacket(acceptPacket);
            }
            else if (e.Packet is PacketConfirmLaborOffer confirmPacket)
            {
                // (this is handled elsewhere via await packet)
                Log.Message("Received labor offer confirmation: " + confirmPacket.Guid);
            }
            else if (e.Packet is PacketReturnLentColonists returnPacket)
            {
                HandleReturnLentColonistsPacket(returnPacket);
            }
            else if (e.Packet is PacketColonistLost lostPacket)
            {
                HandleColonistLostPacket(lostPacket);
            }
        }

        private void HandleAcceptOfferPacket(PacketAcceptLaborOffer packet)
        {
            LaborOffer offer = Offers.FirstOrDefault(o => o.Guid == packet.Guid);
            if (offer == null)
            {
                Log.Warn($"Player accepted to accept a non-existent offer ({packet.Guid}).");
                return;
            }

            if (!packet.Accept)
            {
                // Other player rejected
                Find.LetterStack.ReceiveLetter($"Labor Offer Rejected ({RimLinkComp.Find().Client.GetName(offer.For)})", "Your offer to lend colonists has been rejected.", LetterDefOf.NegativeEvent);
                return;
            }

            bool fulfill = offer.LenderCanStillFulfill();

            Log.Message($"Received acceptance of labor offer {offer.Guid}. Fulfill = {fulfill}");

            // Add as active labor offer
            RimLinkComp.Find().ActiveLaborOffers.Add(offer);

            Client.SendPacket(new PacketConfirmLaborOffer
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

        private void HandleReturnLentColonistsPacket(PacketReturnLentColonists packet)
        {
            LaborOffer offer = RimLinkComp.Find().ActiveLaborOffers.FirstOrDefault(o => o.Guid == packet.Guid);
            if (offer == null)
            {
                Log.Warn("Attempt to return colonists for an unknown labor offer! " + packet.Guid);
                return;
            }

            Log.Message($"Returning lent colonists from {RimLinkComp.Find().Client.GetName(offer.For)}");
            offer.ReturnedColonistsReceived(packet);
        }

        private void HandleColonistLostPacket(PacketColonistLost packet)
        {
            Pawn lostPawn = null;
            LaborOffer activeOffer = null;
            foreach (LaborOffer offer in Client.RimLinkComp.ActiveLaborOffers)
            {
                foreach (Pawn pawn in offer.Colonists)
                {
                    if (pawn.TryGetComp<PawnGuidThingComp>().Guid == packet.PawnGuid)
                    {
                        lostPawn = pawn;
                        activeOffer = offer;
                        break;
                    }
                }
            }

            if (lostPawn == null)
            {
                Log.Warn($"Received lost colonist packet for unknown pawn.");
                return;
            }

            switch (packet.How)
            {
                case PacketColonistLost.LostType.Dead:
                    lostPawn.Kill(null);
                    Find.LetterStack.ReceiveLetter("Killed on duty: " + lostPawn.Name,
                        $"{lostPawn.NameFullColored}, who you lent to {RimLinkComp.Instance.Client.GetName(activeOffer.From)}, has been died.",
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketColonistLost.LostType.Imprisoned:
                    Find.LetterStack.ReceiveLetter("Imprisoned: " + lostPawn.Name,
                        $"{lostPawn.NameFullColored}, who you lent to {RimLinkComp.Instance.Client.GetName(activeOffer.From)}, has been imprisoned.",
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketColonistLost.LostType.Gone:
                    Find.LetterStack.ReceiveLetter("Missing: " + lostPawn.Name,
                        $"{RimLinkComp.Instance.Client.GetName(activeOffer.From)} has lost {lostPawn.NameFullColored}.",
                        LetterDefOf.NegativeEvent);
                    break;
            }
        }
    }
}
