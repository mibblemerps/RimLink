using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestPart_WorkTypeDefDisabled : QuestPartActivable
    {
        public List<Pawn> Pawns = new List<Pawn>();
        public List<WorkTypeDef> DisabledWorkTypeDefs = new List<WorkTypeDef>();

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);
            ClearPawnWorkTypesAndSkillsCache();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ClearPawnWorkTypesAndSkillsCache();
        }

        private void ClearPawnWorkTypesAndSkillsCache()
        {
            foreach (var pawn in Pawns)
            {
                if (pawn != null)
                {
                    pawn.Notify_DisabledWorkTypesChanged();
                    pawn.skills?.Notify_SkillDisablesChanged();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref Pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Collections.Look(ref DisabledWorkTypeDefs, "disabledWorkDefs", LookMode.Def, Array.Empty<object>());
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            Pawns.RemoveAll(x => x == null);
        }

        public static IEnumerable<QuestPart_WorkTypeDefDisabled> GetWorkDefDisabledQuestPart(Pawn pawn)
        {
            foreach (var questPart in MissionUtil.GetQuestPart<QuestPart_WorkTypeDefDisabled>(pawn, (part, p) => part.Pawns.Contains(p)))
            {
                yield return questPart;
            }
        }
    }
}
