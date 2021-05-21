using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PlayerTrade.Missions.Quest
{
    public class QuestPart_ReturnColonists : QuestPart
    {
        [NoTranslate]
        public string InSignal;
        [NoTranslate]
        public string Guid;
        public Thing Shuttle;
        public bool MainGroup = true;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag != InSignal)
                return;

            CompShuttle shuttleComp = Shuttle.TryGetComp<CompShuttle>();

            Log.Message($"Return colonists. Required things loaded = {shuttleComp.AllRequiredThingsLoaded}, Contents = \"{shuttleComp.Transporter.innerContainer.ContentsString}\"");

            MissionOffer offer = RimLinkComp.Instance.Get<MissionSystem>().GetOffer(Guid);
            if (offer == null)
            {
                Log.Warn($"Couldn't find labor offer: {Guid}");
                return;
            }

            var pawns = shuttleComp.Transporter.innerContainer.Where(thing => thing is Pawn).Cast<Pawn>().ToList();
            offer.ReturnColonists(pawns, MainGroup);

            QuestGen.slate.Set("returned_pawns", pawns);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref InSignal, "in_signal");
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_References.Look(ref Shuttle, "shuttle");
        }
    }
}
