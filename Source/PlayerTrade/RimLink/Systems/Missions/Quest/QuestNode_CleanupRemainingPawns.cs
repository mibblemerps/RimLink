using System.Collections.Generic;
using RimWorld.QuestGen;
using Verse;

namespace RimLink.Systems.Missions.Quest
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
                Pawns = new List<Pawn>(pawns.GetValue(slate))
            });
        }

        protected override bool TestRunInt(Slate slate) => true;
    }
}
