using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Labor.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.Labor
{
    public class QuestPart_ReturnColonists : QuestPart
    {
        [NoTranslate]
        public string InSignal;
        [NoTranslate]
        public string Guid;
        public Thing Shuttle;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag != InSignal)
                return;

            CompShuttle shuttleComp = Shuttle.TryGetComp<CompShuttle>();

            Log.Message($"Return colonists. Required things loaded = {shuttleComp.AllRequiredThingsLoaded}, Contents = \"{shuttleComp.Transporter.innerContainer.ContentsString}\"");

            LaborOffer offer = RimLinkComp.Find().ActiveLaborOffers.First(o => o.Guid == Guid);
            if (offer == null)
            {
                Log.Warn($"Couldn't find labor offer: {Guid}");
                return;
            }

            var pawns = shuttleComp.Transporter.innerContainer.Where(thing => thing is Pawn).Cast<Pawn>().ToList();
            offer.ReturnColonists(pawns);
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
