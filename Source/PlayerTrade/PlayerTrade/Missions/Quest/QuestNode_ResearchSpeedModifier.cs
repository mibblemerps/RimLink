using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestNode_ResearchSpeedModifier : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> inSignalEnable;
        public SlateRef<IEnumerable<Pawn>> pawns;
        public SlateRef<float> multiplier;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            var questPart = new QuestPart_ResearchSpeedModifier
            {
                inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ??
                           slate.Get<string>("inSignal"),
                Pawns = new List<Pawn>(pawns.GetValue(slate)),
                Multiplier = multiplier.GetValue(slate)
            };

            QuestGen.quest.AddPart(questPart);
        }

        protected override bool TestRunInt(Slate slate) => true;
    }
}
