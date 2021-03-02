using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Labor
{
    public class QuestNode_CleanupRemainingPawns : QuestNode
    {
        public SlateRef<string> guid;
        public SlateRef<IEnumerable<Pawn>> pawns;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            QuestGen.quest.AddPart(new QuestPart_CleanupRemainingPawns
            {
                Guid = guid.GetValue(slate),
                Pawns = pawns.GetValue(slate)
            });
        }

        protected override bool TestRunInt(Slate slate) => true;
    }
}
