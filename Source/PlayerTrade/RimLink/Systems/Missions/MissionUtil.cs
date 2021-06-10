using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Core;
using RimLink.Systems.Missions.MissionWorkers;
using RimWorld;
using Verse;

namespace RimLink.Systems.Missions
{
    public static class MissionUtil
    {
        public static void PresentLendColonistOffer(MissionOffer offer)
        {
            Letter letter = new ChoiceLetter_LaborOffer(offer);
            letter.ID = Find.UniqueIDsManager.GetNextLetterID();
            Find.LetterStack.ReceiveLetter(letter);
        }

        /// <summary>
        /// Try to find the labor offer associated with this pawn.<br />
        /// This first checks the <see cref="LentColonistComp"/>, if that fails it'll check the pawn GUID in all labor offers.
        /// </summary>
        public static MissionOffer FindLaborOffer(this Pawn pawn)
        {
            var lentColonistComp = pawn.TryGetComp<LentColonistComp>();
            if (lentColonistComp?.MissionOffer != null)
                return lentColonistComp.MissionOffer;

            var guid = pawn.TryGetComp<PawnGuidComp>().Guid;
            return RimLink.Instance.Get<MissionSystem>().Offers.FirstOrDefault(offer =>
            {
                foreach (Pawn offerPawn in offer.Colonists)
                {
                    if (offerPawn.TryGetComp<PawnGuidComp>().Guid == guid)
                        return true;
                }

                return false;
            });
        }

        public delegate bool FindQuestPartPredicate<T>(T part, Pawn pawn) where T : QuestPart;
        /// <summary>
        /// Find a quest part. If activable that part will be checked that it is activated.
        /// </summary>
        /// <typeparam name="T">Quest part type</typeparam>
        /// <param name="pawn">Pawn to pass to the predicate</param>
        /// <param name="predicate">A predicate to see if this quest part applies to the provided pawn</param>
        /// <returns>Quest part</returns>
        public static IEnumerable<T> GetQuestPart<T>(Pawn pawn, FindQuestPartPredicate<T> predicate) where T : QuestPart
        {
            foreach (RimWorld.Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest.State != QuestState.Ongoing)
                    continue;

                foreach (QuestPart part in quest.PartsListForReading)
                {
                    if (part is T tPart && predicate(tPart, pawn) && part is QuestPartActivable activable && activable.State == QuestPartState.Enabled)
                        yield return tPart;
                }
            }
        }

        public static IEnumerable<Pawn> GetPawnsAvailableForMissions(PlayerMissionDef missionDef = null)
        {
            MissionWorker worker = null;
            if (missionDef != null)
                worker = missionDef.CreateWorker(null);

            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                if (pawn.Dead || pawn.health.Downed)
                    continue;

                if (worker != null && !worker.IsColonistEligible(pawn))
                    continue; // Not eligible according to mission worker

                yield return pawn;
            }
        }

        public static MissionOffer SendMission(Player target, PlayerMissionDef missionDef, IEnumerable<Pawn> pawns, MissionWorker worker, float days)
        {
            if (worker == null)
                worker = missionDef.CreateWorker(null); // was give the worker the LaborOffer instance shortly

            if (!Type.GetType(missionDef.workerClass).IsInstanceOfType(worker))
                throw new ArgumentException($"PlayerMissionWorker doesn't match worker type defined in the provided mission def.", nameof(worker));

            var offer = new MissionOffer
            {
                Guid = Guid.NewGuid().ToString(),
                For = target.Guid,
                From = RimLink.Instance.Guid,
                Colonists = new List<Pawn>(pawns),
                Days = days,
                Fresh = true,
                MissionDef = missionDef,
                MissionWorker = worker
            };

            worker.Offer = offer;

            offer.OfferSend();

            RimLink.Instance.Get<MissionSystem>().AddOffer(offer);
            RimLink.Instance.Client.SendPacket(offer.ToPacket());

            return offer;
        }
    }
}
