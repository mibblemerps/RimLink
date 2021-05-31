using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestNode_DelayCanEndEarly : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> inSignalEnable;
        [NoTranslate]
        public SlateRef<string> inSignalDisable;
        [NoTranslate]
        public SlateRef<string> inSignalEnd;
        [NoTranslate]
        public SlateRef<string> outSignalComplete;
        public SlateRef<string> expiryInfoPart;
        public SlateRef<string> expiryInfoPartTip;
        public SlateRef<string> inspectString;
        public SlateRef<IEnumerable<ISelectable>> inspectStringTargets;
        public SlateRef<int> delayTicks;
        public SlateRef<IntRange?> delayTicksRange;
        public SlateRef<bool> isQuestTimeout;
        public SlateRef<bool> reactivatable;
        public QuestNode node;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            QuestPart_DelayCanEndEarly part = new QuestPart_DelayCanEndEarly();

            // Delay
            if (delayTicksRange.GetValue(slate).HasValue)
                part.delayTicks = delayTicksRange.GetValue(slate).Value.RandomInRange;
            else
                part.delayTicks = delayTicks.GetValue(slate);

            part.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
            part.inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable.GetValue(slate));
            part.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnd.GetValue(slate));
            part.reactivatable = reactivatable.GetValue(slate);

            // Inspect stuffs
            if (!inspectStringTargets.GetValue(slate).EnumerableNullOrEmpty())
            {
                part.inspectString = inspectString.GetValue(slate);
                part.inspectStringTargets = new List<ISelectable>();
                part.inspectStringTargets.AddRange(inspectStringTargets.GetValue(slate));
            }

            // Quest timeout
            if (isQuestTimeout.GetValue(slate))
            {
                part.isBad = true;
                part.expiryInfoPart = "QuestExpiresIn".Translate();
                part.expiryInfoPartTip = "QuestExpiresOn".Translate();
            }
            else
            {
                part.expiryInfoPart = expiryInfoPart.GetValue(slate);
                part.expiryInfoPartTip = expiryInfoPartTip.GetValue(slate);
            }

            if (node != null)
                QuestGenUtility.RunInnerNode(node, part);
            if (!outSignalComplete.GetValue(slate).NullOrEmpty())
                part.outSignalsCompleted.Add(QuestGenUtility.HardcodedSignalWithQuestID(outSignalComplete.GetValue(slate)));

            QuestGen.quest.AddPart(part);
        }

        protected override bool TestRunInt(Slate slate) => node == null || node.TestRun(slate);
    }
}
