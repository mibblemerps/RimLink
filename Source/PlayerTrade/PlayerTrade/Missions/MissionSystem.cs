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
                Find.LetterStack.ReceiveLetter(
                    "Rl_MissionOfferRejected".Translate(offer.MissionDef.LabelCap, offer.For.GuidToName()),
                    "Rl_MissionOfferRejectedDesc".Translate(), LetterDefOf.NegativeEvent);
                return;
            }

            bool fulfill = offer.CanFulfillAsSender;

            Log.Message($"Rl_Received acceptance of mission offer {offer.Guid}. Fulfill = {fulfill}");

            Client.SendPacket(new PacketConfirmMissionOffer
            {
                For = offer.For,
                Guid = offer.Guid,
                Confirm = fulfill
            });

            if (fulfill)
            {
                offer.FulfillAsSender(Find.CurrentMap);
                Find.LetterStack.ReceiveLetter(
                    "Rl_MissionOfferAccepted".Translate(offer.MissionDef.LabelCap, offer.For.GuidToName()),
                    "Rl_MissionOfferAcceptedDesc".Translate(
                        offer.Colonists.Count == 1 ? "colonist" : "colonists",
                        offer.Days.ToString().Colorize(ColoredText.DateTimeColor),
                        "day" + offer.Days.MaybeS()
                        ), LetterDefOf.PositiveEvent);
            }
            else
            {
                Find.LetterStack.ReceiveLetter("Rl_MissionOfferAborted".Translate(offer.For.GuidToName()),
                    "Rl_MissionOfferAbortedDesc".Translate(), LetterDefOf.NegativeEvent);
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
                    Find.LetterStack.ReceiveLetter("Rl_KilledOnDuty".Translate(pawn.NameShortColored, activeOffer.From.GuidToName()),
                        "Rl_KilledOnDutyDesc".Translate(pawn.NameFullColored, activeOffer.From.GuidToName(true)),
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Imprisoned:
                    Find.LetterStack.ReceiveLetter("Rl_Imprisoned".Translate(pawn.NameShortColored, activeOffer.From.GuidToName()),
                        "Rl_ImprisonedDesc".Translate(pawn.NameFullColored, activeOffer.From.GuidToName(true)),
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Gone:
                    Find.LetterStack.ReceiveLetter("Rl_Missing".Translate(pawn.NameShortColored, activeOffer.From.GuidToName()),
                        "Rl_MissingDesc".Translate(pawn.NameFullColored, activeOffer.From.GuidToName(true)),
                        LetterDefOf.NegativeEvent);
                    break;

                case PacketLentColonistUpdate.ColonistEvent.Escaped:
                    Find.LetterStack.ReceiveLetter("Rl_Escaped".Translate(pawn.NameShortColored, activeOffer.From.GuidToName()),
                        "Rl_EscapedDesc".Translate(pawn.NameFullColored, activeOffer.From.GuidToName(true)),
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
