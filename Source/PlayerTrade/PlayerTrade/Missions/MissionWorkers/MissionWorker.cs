using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Util;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace PlayerTrade.Missions.MissionWorkers
{
    public class MissionWorker : IExposable, IPacketable
    {
        public static LetterDef MissionOfferLetterDef =>
            _missionOfferLetterDef ??
            (_missionOfferLetterDef = DefDatabase<LetterDef>.GetNamed("MissionOffer"));

        private static LetterDef _missionOfferLetterDef;

        public MissionOffer Offer;

        /// <summary>
        /// Used by the sender to store the market values of the pawnns at the time the offer was made.
        /// </summary>
        private Dictionary<Pawn, float> _senderPawnMarketValues = new Dictionary<Pawn, float>();
        private List<Pawn> _workingListKeysSenderPawnMarketValues = new List<Pawn>();
        private List<float> _workingListValuesSenderPawnMarketValues = new List<float>();

        /// <summary>
        /// Is the colonist eligible for this mission? For example, a research mission might require they be capable of intellectual.
        /// </summary>
        public virtual bool IsColonistEligible(Pawn pawn)
        {
            return true; // No special requirements for a basic mission
        }

        public virtual bool CanFulfillAsSender()
        {
            foreach (Pawn pawn in Offer.Colonists)
            {
                if (pawn.Dead || !pawn.Spawned || pawn.health.Downed)
                    return false;

                float intendedMarketValue = _senderPawnMarketValues[pawn];
                if (Mathf.Abs(pawn.MarketValue - intendedMarketValue) > MissionOffer.MarketValueVarianceAllowed)
                    return false;
            }

            return true;
        }

        public virtual bool CanFulfillAsReceiver()
        {
            return true;
        }

        /// <summary>
        /// <b>Step 1.</b> Offer sent from sender. This is called just before the offer is sent.
        /// </summary>
        public virtual void OfferSend()
        {
            foreach (Pawn pawn in Offer.Colonists)
                _senderPawnMarketValues.Add(pawn, pawn.MarketValue);
        }

        /// <summary>
        /// <b>Step 2.</b> Offer received from sender.
        /// </summary>
        public virtual void OfferReceived()
        {
            Letter letter = new ChoiceLetter_MissionOffer(Offer, true);
            letter.ID = Find.UniqueIDsManager.GetNextLetterID();
            Find.LetterStack.ReceiveLetter(letter);
        }

        /// <summary>
        /// <b>Step 3.</b> Fulfill mission on sender's side.
        /// </summary>
        public virtual void FulfillAsSender(Map map) { }

        /// <summary>
        /// <b>Step 4.</b> Start mission on receiver's end
        /// </summary>
        public virtual void FulfillAsReceiver(Map map)
        {
            // Spawn pawns
            Log.Verbose("Fulfilling as receiver: spawning pawns");
            ArrivalUtil.Arrive(map, Offer.MissionDef.arrivalMethod, Offer.Colonists.ToArray());

            // Create quest
            Log.Verbose("Fulfilling as receiver: creating quest");
            var slate = new Slate();
            SetSlateVars(slate);
            RimWorld.Quest quest = QuestGen.Generate(Offer.MissionDef.QuestScriptDef, slate);
            Find.QuestManager.Add(quest);
        }

        /// <summary>
        /// <b>Step 5.</b> Colonists returned.
        /// </summary>
        /// <param name="pawns">Pawns that were just returned</param>
        /// <param name="moreLeft">Are there more pawns to be returned?</param>
        /// <param name="mainGroup">Is this the main group of returning colonists? If colonists are missing from this group the receiver has failed.</param>
        /// <param name="escaped">Did these colonists escape instead of just being sent home.</param>
        public virtual void ReturnedColonistsReceived(List<Pawn> pawns, bool moreLeft, bool mainGroup, bool escaped)
        {
            if (escaped)
                return; // We don't handle escapes here

            if (!moreLeft) // If there are no more left, treat this as the main group since it'll be the last. Otherwise the mission will never actually end
                mainGroup = true;

            var args = new List<NamedArgument>
            {
                new NamedArgument(Offer.For.GuidToName(true), "player"),
                new NamedArgument(Offer.MissionDef.LabelCap, "mission"),
                new NamedArgument(pawns.Count == 1 ? "colonist" : "colonists", "colonists"),
            };

            if (pawns.Count > 0)
            {
                args.Add(new NamedArgument(pawns.First().NameShortColored, "colonist_name_short"));
                args.Add(new NamedArgument(pawns.First().NameFullColored, "colonist_name_long"));
            }

            LookTargets targets = new LookTargets(pawns);

            if (mainGroup)
            {
                if (!moreLeft)
                {
                    // All returned. Success
                    Find.LetterStack.ReceiveLetter(Offer.MissionDef.allReturnedLetterTitle.Formatted(args.ToArray()),
                        Offer.MissionDef.allReturnedLetterBody.Formatted(args.ToArray()),
                        LetterDefOf.PositiveEvent, targets);
                }
                else
                {
                    // Not all returned in main group. Failed
                    Find.LetterStack.ReceiveLetter(Offer.MissionDef.notReturnedLetterTitle.Formatted(args.ToArray()),
                        Offer.MissionDef.notReturnedLetterBody.Formatted(args.ToArray()),
                        LetterDefOf.NegativeEvent, targets);
                }
            }
            else
            {
                // Early return
                Find.LetterStack.ReceiveLetter(Offer.MissionDef.returnedEarlyLetterTitle.Formatted(args.ToArray()),
                    Offer.MissionDef.returnedEarlyLetterBody.Formatted(args.ToArray()),
                    LetterDefOf.PositiveEvent, targets);
            }
        }

        public virtual void SetSlateVars(Slate slate)
        {
            slate.Set("guid", Offer.Guid);
            slate.Set("from", Offer.From.GuidToName());
            slate.Set("days", Offer.Days);
            slate.Set("shuttle_arrival_ticks", Mathf.RoundToInt(Mathf.Max(0, (Offer.Days - 0.5f) * 60000f))); // shuttle arrives 12 hours early
            slate.Set("shuttle_leave_ticks", Mathf.RoundToInt(Offer.Days * 60000f));
            slate.Set("pawns", Offer.Colonists);
            slate.Set("pawn_count", Offer.Colonists.Count);
            slate.Set("home_faction", RimLinkComp.Instance.PlayerFactions[Offer.From]);

            slate.Set("pawnLabelSingular",  Offer.MissionDef.colonistsNounSingular);
            slate.Set("pawnLabelPlural", Offer.MissionDef.colonistsNounPlural);
            slate.Set("pawnLabelSingularCap", Offer.MissionDef.colonistsNounSingular.CapitalizeFirst());
            slate.Set("pawnLabelPluralCap", Offer.MissionDef.colonistsNounPlural.CapitalizeFirst());

            List<Rule> rules = new List<Rule>();

            rules.Add(new Rule_String("pawnLabelSingular", Offer.MissionDef.colonistsNounSingular));
            rules.Add(new Rule_String("pawnLabelPlural", Offer.MissionDef.colonistsNounPlural));
            rules.Add(new Rule_String("pawnLabelSingularCap", Offer.MissionDef.colonistsNounSingular.CapitalizeFirst()));
            rules.Add(new Rule_String("pawnLabelPluralCap", Offer.MissionDef.colonistsNounPlural.CapitalizeFirst()));

            rules.AddRange(GrammarUtility.RulesForPawn("firstPawn", Offer.Colonists.First()));

            QuestGen.AddQuestNameRules(rules);
            QuestGen.AddQuestDescriptionRules(rules);
            QuestGen.AddQuestContentRules(rules);
        }

        /// <summary>
        /// Update called every game update as long as the mission is active.
        /// </summary>
        public virtual void Update()
        {

        }

        public virtual void ExposeData()
        {
            Scribe_Collections.Look(ref _senderPawnMarketValues, "pawn_market_values", LookMode.Reference, LookMode.Value, ref _workingListKeysSenderPawnMarketValues, ref _workingListValuesSenderPawnMarketValues);
        }

        public virtual void Write(PacketBuffer buffer) {}

        public virtual void Read(PacketBuffer buffer) {}
    }
}
