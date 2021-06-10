using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimLink.Systems.Missions.MissionWorkers
{
    public class HolidayMissionWorker : MissionWorker
    {
        public override void ReturnColonists(List<Pawn> pawns, bool mainGroup, bool escaped)
        {
            base.ReturnColonists(pawns, mainGroup, escaped);

            if (!escaped)
            {
                // Give went on holiday thought
                foreach (Pawn pawn in pawns)
                {
                    Thought_Memory thought = ThoughtMaker.MakeThought(ThoughtDef.Named("HadHoliday"), GetHolidayHappinessIndex(pawn) ?? 3);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }
        }

        public static int? GetHolidayHappinessIndex(Pawn p, bool skipChecks = false)
        {
            if (!skipChecks)
            {
                if (!RimLinkMod.Active || p.IsPrisonerOfColony || p.MapHeld == null) return null;

                // Check this pawn is on holiday
                LentColonistComp comp = p.TryGetComp<LentColonistComp>();
                if (comp.MissionOffer == null || comp.MissionOffer.MissionDef.defName != "Holiday") return null;
            }

            // Calculate mood stage based on wealth/expectations
            float wealth = p.MapHeld.wealthWatcher.WealthTotal;
            if (wealth <= ExpectationDefOf.VeryLow.maxMapWealth)
                return 0;
            if (wealth <= ExpectationDefOf.Low.maxMapWealth)
                return 1;
            if (wealth <= ExpectationDefOf.Moderate.maxMapWealth)
                return 2;
            if (wealth <= ExpectationDefOf.High.maxMapWealth)
                return 3;
            if (wealth <= ExpectationDefOf.SkyHigh.maxMapWealth)
                return 4;
            
            return 3; // fallback
        }

        [DebugAction("RimLink", "Had Holiday Thought", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DebugGiveHadHolidayThought(Pawn pawn)
        {
            Thought_Memory thought = ThoughtMaker.MakeThought(ThoughtDef.Named("HadHoliday"), GetHolidayHappinessIndex(pawn, true) ?? 0);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought);

        }
    }
}