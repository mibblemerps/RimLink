using PlayerTrade.Labor.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.Labor
{
    public class QuestPart_NotifyColonistLost : QuestPart
    {
        public PacketColonistLost.LostType How;
        [NoTranslate]
        public string InSignal;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag != InSignal)
                return;

            Pawn pawn = signal.args.GetArg<Pawn>("SUBJECT");

            Log.Message($"Colonist lost {pawn.Name}. Reason: {How}");

            // Find related labor offer
            LaborOffer offer = null;
            foreach (LaborOffer o in RimLinkComp.Instance.ActiveLaborOffers)
            {
                foreach (Pawn p in o.Colonists)
                {
                    if (p == pawn)
                        offer = o;
                }
            }

            if (offer == null)
            {
                // Couldn't find labor offer that colonist belongs to.
                // This means the labor offer is already completed (failed, colonists returned, whatever) so we don't care and just ignore this.
                return;
            }

            RimLinkComp.Instance.Client.SendPacket(new PacketColonistLost
            {
                For = offer.From,
                How = How,
                PawnGuid = pawn.TryGetComp<PawnGuidThingComp>().Guid
            });
        }
    }
}
