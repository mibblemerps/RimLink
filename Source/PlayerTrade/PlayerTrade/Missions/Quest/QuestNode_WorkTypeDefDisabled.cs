using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestNode_WorkTypeDefDisabled : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> inSignalEnable;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public SlateRef<bool> invert;
        public SlateRef<IEnumerable<WorkTypeDef>> workTypeDefs;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            if (pawns.GetValue(slate) == null)
                return;

            var disabled = new List<WorkTypeDef>(workTypeDefs.GetValue(slate));

            if (invert.GetValue(slate))
            {
                var enabled = new List<WorkTypeDef>(disabled);
                disabled.Clear();
                foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    if (!enabled.Contains(def))
                        disabled.Add(def);
                }
            }

            QuestPart_WorkTypeDefDisabled partWorkDisabled = new QuestPart_WorkTypeDefDisabled();
            partWorkDisabled.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
            partWorkDisabled.Pawns.AddRange(pawns.GetValue(slate));
            partWorkDisabled.DisabledWorkTypeDefs = disabled;
            QuestGen.quest.AddPart(partWorkDisabled);
        }

        protected override bool TestRunInt(Slate slate) => true;
    }
}
