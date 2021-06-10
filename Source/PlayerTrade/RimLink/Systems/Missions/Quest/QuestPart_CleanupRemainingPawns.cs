using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimLink.Systems.Missions.Quest
{
    public class QuestPart_CleanupRemainingPawns : QuestPart
    {
        [NoTranslate]
        public string Guid;
        public List<Pawn> Pawns;

        public override void Cleanup()
        {
            base.Cleanup();

            List<Pawn> returned = QuestGen.slate.Get("returned_pawns", new List<Pawn>());

            MissionOffer offer = RimLink.Instance.Get<MissionSystem>().GetOffer(Guid);
            if (offer == null)
            {
                Log.Error("Labor offer not found!");
                return;
            }

            // Make remaining pawns leave and set their faction to their home faction
            foreach (Pawn pawn in Pawns)
            {
                if (returned.Contains(pawn))
                    continue;
                if (pawn.Dead)
                    continue;

                pawn.TryGetComp<LentColonistComp>().Notify_FailedToBeReturned(quest);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Collections.Look(ref Pawns, "pawns", LookMode.Reference);
        }
    }
}
