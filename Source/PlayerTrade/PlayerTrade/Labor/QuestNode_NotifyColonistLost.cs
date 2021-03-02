using PlayerTrade.Labor.Packets;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Labor
{
    public class QuestNode_NotifyColonistLost : QuestNode
    {
        public SlateRef<PacketLentColonistUpdate.ColonistEvent> how;

        public SlateRef<string> inSignal;

        protected override bool TestRunInt(Slate slate) => true;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            QuestGen.quest.AddPart(new QuestPart_NotifyColonistLost
            {
                InSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ??
                           slate.Get<string>("inSignal"),
                How = how.GetValue(slate)
            });
        }
    }
}
