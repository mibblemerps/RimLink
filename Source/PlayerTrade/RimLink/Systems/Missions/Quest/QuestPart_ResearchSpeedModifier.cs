using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimLink.Systems.Missions.Quest
{
    public class QuestPart_ResearchSpeedModifier : QuestPartActivable
    {
        public List<Pawn> Pawns = new List<Pawn>();
        public float Multiplier = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref Pawns, "pawns", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look(ref Multiplier, "multiplier", 1f);
        }
    }
}
