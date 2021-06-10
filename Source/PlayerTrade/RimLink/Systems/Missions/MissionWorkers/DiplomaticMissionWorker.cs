using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimLink.Systems.Missions.MissionWorkers
{
    public class DiplomaticMissionWorker : MissionWorker
    {
        public const int MinSeniority = 100;

        public override bool IsColonistEligible(Pawn pawn)
        {
            if (!ModLister.RoyaltyInstalled)
                return false;

            return base.IsColonistEligible(pawn) && pawn.royalty?.GetCurrentTitleInFaction(Faction.Empire)?.def?.seniority >= MinSeniority;
        }

        public override void SetSlateVars(Slate slate)
        {
            Pawn highestRanking = Offer.Colonists.First();
            RoyalTitleDef highestTitle = null;
            foreach (Pawn pawn in Offer.Colonists)
            {
                RoyalTitleDef title = pawn.royalty?.GetCurrentTitle(Faction.Empire);
                if (title != null && (highestTitle == null || title.seniority > highestTitle.seniority))
                {
                    highestTitle = title;
                    highestRanking = pawn;
                }
            }

            if (highestTitle == null)
            {
                Log.Error("Couldn't find royal title on mission pawns!");
                return;
            }

            slate.Set("royal_title", highestTitle.LabelCap);
            slate.Set("royal_pawn", highestRanking.LabelCap);

            base.SetSlateVars(slate);
        }
    }
}
