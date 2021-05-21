using System;
using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Missions.MissionWorkers;
using PlayerTrade.Missions.Packets;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Missions
{
    public class MissionSystem : ISystem
    {
        public List<Pawn> EscapingLentColonists = new List<Pawn>();
        
        public Client Client;

        /// <summary>
        /// List of all offers that are pending or active. Use <see cref="AddOffer"/> to add new offers.
        /// </summary>
        public IEnumerable<MissionOffer> Offers => _offers;

        private List<MissionOffer> _offers = new List<MissionOffer>();
        
        private float _queuedResearch;
        private float _lastResearchUpdate;

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        public void AddOffer(MissionOffer offer)
        {
            if (_offers.Any(o => o.Guid == offer.Guid))
            {
                Log.Warn("Attempt to add offer that's already added! " + offer.Guid);
                return;
            }
            
            _offers.Add(offer);
        }

        public MissionOffer GetOffer(string guid)
        {
            return Offers.FirstOrDefault(offer => offer.Guid == guid);
        }
        
        public void Update()
        {
            // Update offers
            foreach (MissionOffer offer in Offers)
            {
                if (offer.Active)
                    offer.Update();
            }

            // Perform queued research
            if (Time.realtimeSinceStartup > _lastResearchUpdate + ResearchMissionWorker.ResearchUpdateInterval)
            {
                _lastResearchUpdate = Time.realtimeSinceStartup;

                ResearchManager research = Find.ResearchManager;
                if (_queuedResearch > 0f && research.currentProj != null)
                {
                    // Do research
                    _queuedResearch -= ResearchUtil.AddResearchPoints(_queuedResearch);
                }
            }
            
            // Try and let escaping colonists escape
            foreach (Pawn escapingLentColonist in EscapingLentColonists)
            {
                var comp = escapingLentColonist.TryGetComp<LentColonistComp>();
                comp.TryEscape();
            }
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketMissionOffer offerPacket)
            {
                MissionOffer offer = MissionOffer.FromPacket(offerPacket);
                AddOffer(offer);
                Log.Message($"Received mission offer {offer.Guid} from {Client.GetName(offer.From)}");
                offer.MissionWorker.OfferReceived();
            }
            else if (e.Packet is PacketAcceptMissionOffer acceptPacket)
            {
                HandleAcceptOfferPacket(acceptPacket);
            }
            else if (e.Packet is PacketConfirmMissionOffer confirmPacket)
            {
                // (this is handled elsewhere via await packet)
                Log.Message("Received mission offer confirmation: " + confirmPacket.Guid);
            }
            else if (e.Packet is PacketReturnLentColonists returnPacket)
            {
                HandleReturnLentColonistsPacket(returnPacket);
            }
            else if (e.Packet is PacketLentColonistUpdate updatePacket)
            {
                HandleColonistUpdatePacket(updatePacket);
            }
            else if (e.Packet is PacketResearch researchPacket)
            {
                // Queue research work
                _queuedResearch += researchPacket.Research;
            }
        }

        private void HandleAcceptOfferPacket(PacketAcceptMissionOffer packet)
        {
            MissionOffer offer = Offers.FirstOrDefault(o => o.Guid == packet.Guid);
            if (offer == null)
            {
                Log.Warn($"Player accepted to accept a non-existent offer ({packet.Guid}).");
                return;
            }

            if (!packet.Accept)
            {
                // Other player rejected
                Find.LetterStack.ReceiveLetter($"{offer.MissionDef.LabelCap} Rejected ({RimLinkComp.Find().Client.GetName(offer.For)})", "Your offer has been rejected.", LetterDefOf.NegativeEvent);
                return;
            }

            bool fulfill = offer.CanFulfillAsSender;

            Log.Message($"Received acceptance of mission offer {offer.Guid}. Fulfill = {fulfill}");

            Client.SendPacket(new PacketConfirmMissionOffer
            {
                For = offer.For,
                Guid = offer.Guid,
                Confirm = fulfill
            });

            if (fulfill)
            {
                offer.FulfillAsSender(Find.CurrentMap);
                Find.LetterStack.ReceiveLetter($"{offer.MissionDef.LabelCap} Offer Accepted ({offer.For.GuidToName()})", $"Your offer to lend colonists has been accepted.\n\n" +
                    $"Your {(offer.Colonists.Count == 1 ? "colonist" : "colonists")} should be returned in {offer.Days.ToString().Colorize(ColoredText.DateTimeColor)} day{offer.Days.MaybeS()}.", LetterDefOf.PositiveEvent);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"{offer.MissionDef.LabelCap} Offer Aborted ({RimLinkComp.Find().Client.GetName(offer.For)})", "The colonists offered are not in the same condition as when they were initially offered.", LetterDefOf.NegativeEvent);
            }
        }

        private void HandleReturnLentColonistsPacket(PacketReturnLentColonists packet)
        {
            MissionOffer offer = RimLinkComp.Instance.Get<MissionSystem>().GetOffer(packet.Guid);
            if (offer == null)
            {
                Log.Warn("Attempt to return colonists for an unknown labor offer! " + packet.Guid);
                return;
            }

            Log.Message($"Returning lent colonists from {offer.For.GuidToName()}");
            offer.ReturnedColonistsReceived(packet);
        }

        private void HandleColonistUpdatePacket(PacketLentColonistUpdate packet)
        {
            if (_offers == null)
            {
                Debug.LogError("offers is null!");
                return;
            }
            
            Pawn pawn = null;
            MissionOffer activeOffer = null;
            foreach (MissionOffer offer in Offers)
            {
                foreach (Pawn p in offer.Colonists)
                {
                    if (p.TryGetComp<PawnGuidThingComp>().Guid == packet.PawnGuid)
                    {
                        pawn = p;
                        activeOffer = offer;
                        break;
                    }
                }
            }

            if (pawn == null)
            {
                Log.Warn($"Received lost colonist packet for unknown pawn.");
                return;
            }

            switch (packet.What)
            {
                case PacketLentColonistUpdate.ColonistEvent.Dead:
                    pawn.Kill(null);
                    Find.LetterStack.ReceiveLetter("Killed on duty: " + pawn.Name,
                        $"{pawn.NameFullColored}, who you lent to {RimLinkComp.Instance.Client.GetName(activeOffer.From)}, has been died.",
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Imprisoned:
                    Find.LetterStack.ReceiveLetter("Imprisoned: " + pawn.Name,
                        $"{pawn.NameFullColored}, who you lent to {RimLinkComp.Instance.Client.GetName(activeOffer.From)}, has been imprisoned.",
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Gone:
                    Find.LetterStack.ReceiveLetter("Missing: " + pawn.Name,
                        $"{RimLinkComp.Instance.Client.GetName(activeOffer.From)} has lost {pawn.NameFullColored}.",
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Escaped:
                    Find.LetterStack.ReceiveLetter("Escaped: " + pawn.Name,
                        $"{pawn.NameFullColored}, who you lent to {RimLinkComp.Instance.Client.GetName(activeOffer.From)}, has managed to flee after not being returned.\n\n" +
                        $"They will try to find their way home.",
                        LetterDefOf.NeutralEvent);
                    break;
            }
        }

        public void ExposeData()
        {
            Scriber.Collection(ref _offers, "missions", LookMode.Deep);
            Scriber.Collection(ref EscapingLentColonists, "escaping_lent_colonists", LookMode.Reference);
            Scribe_Values.Look(ref _queuedResearch, "queued_research");
        }
    }
}
