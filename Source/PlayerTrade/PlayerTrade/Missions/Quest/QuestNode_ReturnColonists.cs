using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestNode_ReturnColonists : QuestNode
    {
        /// <summary>
        /// Labor offer guid
        /// </summary>
        public SlateRef<string> guid;
        public SlateRef<Thing> shuttle;

        public SlateRef<string> inSignal;

        protected override bool TestRunInt(Slate slate) => true;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            var questPart = new QuestPart_ReturnColonists
            {
                InSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ??
                           slate.Get<string>("inSignal"),
                Shuttle = shuttle.GetValue(slate),
                Guid = guid.GetValue(slate)
            };

            QuestGen.quest.AddPart(questPart);
        }
    }
}
