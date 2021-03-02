using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Labor
{
    public class QuestPart_CleanupRemainingPawns : QuestPart
    {
        [NoTranslate]
        public string Guid;
        public IEnumerable<Pawn> Pawns;

        public override void Cleanup()
        {
            base.Cleanup();

            List<Pawn> returned = QuestGen.slate.Get("returned_pawns", new List<Pawn>());

            LaborOffer offer = RimLinkComp.Instance.ActiveLaborOffers.FirstOrDefault(o => o.Guid == Guid);
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
    }
}
