using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade
{
    /// <summary>
    /// Same as <see cref="RimWorld.QuestGen.QuestNode_GenerateShuttle"/> except allows specifying more specifically what <see cref="Verse.Thing"/> must be loaded.
    /// </summary>
    public class QuestNode_GenerateShuttleForSpecificThings : QuestNode
    {
        // Make compatible with same slate references as default generate shuttle quest node
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<IEnumerable<Pawn>> requiredPawns;
        public SlateRef<IEnumerable<Thing>> requiredThings;
        public SlateRef<int> requireColonistCount;
        public SlateRef<bool> acceptColonists;
        public SlateRef<bool> onlyAcceptColonists;
        public SlateRef<bool> onlyAcceptHealthy;
        public SlateRef<bool> leaveImmediatelyWhenSatisfied;
        public SlateRef<bool> dropEverythingIfUnsatisfied;
        public SlateRef<bool> dropEverythingOnArrival;
        public SlateRef<Faction> owningFaction;
        public SlateRef<bool> permitShuttle;
        public SlateRef<bool> hideControls;

        protected override void RunInt()
        {
            if (!ModLister.RoyaltyInstalled)
            {
                Log.Error("Royalty required");
                return;
            }

            Slate slate = QuestGen.slate;

            Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle);
            if (owningFaction.GetValue(slate) != null)
                thing.SetFaction(this.owningFaction.GetValue(slate));

            // Create shuttle from slate ref data
            // This is almost completely the same as what happens in the default GenerateShuttle quest node - as the maintain comp
            CompShuttle comp = thing.TryGetComp<CompShuttle>();
            if (requiredPawns.GetValue(slate) != null)
                comp.requiredPawns.AddRange(requiredPawns.GetValue(slate));
            comp.acceptColonists = acceptColonists.GetValue(slate);
            comp.onlyAcceptColonists = onlyAcceptColonists.GetValue(slate);
            comp.onlyAcceptHealthy = onlyAcceptHealthy.GetValue(slate);
            comp.requiredColonistCount = requireColonistCount.GetValue(slate);
            comp.dropEverythingIfUnsatisfied = dropEverythingIfUnsatisfied.GetValue(slate);
            comp.leaveImmediatelyWhenSatisfied = leaveImmediatelyWhenSatisfied.GetValue(slate);
            comp.dropEverythingOnArrival = dropEverythingOnArrival.GetValue(slate);
            comp.permitShuttle = permitShuttle.GetValue(slate);
            comp.hideControls = hideControls.GetValue(slate);
            QuestGen.slate.Set(storeAs.GetValue(slate), thing);

            if (requiredThings.GetValue(slate) != null)
            {
                // Transporter time
                CompTransporter transporter = comp.Transporter;
                foreach (Thing loadThing in requiredThings.GetValue(slate))
                {
                    transporter.AddToTheToLoadList(new TransferableOneWay
                    {
                        things = new List<Thing>{loadThing}
                    }, 1);
                }
            }
        }


        protected override bool TestRunInt(Slate slate) => true;
    }
}
