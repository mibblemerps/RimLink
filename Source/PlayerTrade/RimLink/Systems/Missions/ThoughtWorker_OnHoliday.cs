using RimLink.Systems.Missions.MissionWorkers;
using RimWorld;
using Verse;

namespace RimLink.Systems.Missions
{
    public class ThoughtWorker_OnHoliday : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            int? happinessIndex = HolidayMissionWorker.GetHolidayHappinessIndex(p);
            if (!happinessIndex.HasValue) return ThoughtState.Inactive;
            return ThoughtState.ActiveAtStage(happinessIndex.Value);
        }
    }
}