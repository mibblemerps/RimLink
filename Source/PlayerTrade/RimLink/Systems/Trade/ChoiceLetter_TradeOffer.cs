using System.Collections.Generic;
using RimLink.Util;
using Verse;

namespace RimLink.Systems.Trade
{
    public class ChoiceLetter_TradeOffer : ChoiceLetter
    {
        public TradeOffer Offer;

        public ChoiceLetter_TradeOffer(TradeOffer offer)
        {
            Offer = offer;

            def = DefDatabase<LetterDef>.GetNamed("PlayerTradeOffer");
            label = "Rl_TradeOffer".Translate(offer.From.GuidToName());
            text = "Rl_TradeOfferDesc".Translate(offer.From.GuidToName(true));
        }

        public ChoiceLetter_TradeOffer() { }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (!Offer.IsForUs)
                {
                    // We sent this offer - don't allow us to "accept" it!
                    yield return Option_Close;
                    yield break;
                }
                
                var accept = new DiaOption("Accept");
                accept.action = Accept;
                accept.resolveTree = true;
                if (Offer == null || !Offer.Fresh)
                    accept.Disable("Offer expired");
                else if (!Offer.CanFulfill(true))
                    accept.Disable("Missing resources");
                yield return accept;

                var reject = new DiaOption("RejectLetter".Translate());
                reject.resolveTree = true;
                reject.action = Reject;
                if (Offer == null || !Offer.Fresh)
                    reject.Disable("Offer expired");
                yield return reject;

                yield return Option_Postpone;
            }
        }

        private void Accept()
        {
            if (!Offer.Fresh)
                return; // Offer not fresh (sanity check)
            Find.LetterStack.RemoveLetter(this);

            Find.WindowStack.Add(new Dialog_TradeIntermission(Offer));

            Offer.Accept();
        }

        private void Reject()
        {
            Find.LetterStack.RemoveLetter(this);
            if (Offer.Fresh)
            {
                Offer.Reject(); // send rejection
                RimLink.Instance.Get<TradeSystem>().ActiveTradeOffers.Remove(Offer);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Offer, "trade_offer");
        }

        public override void OpenLetter()
        {
            DiaNode nodeRoot = new DiaNode(text);
            nodeRoot.options.AddRange(Choices);
            Find.WindowStack.Add(new Dialog_TradeOffer(nodeRoot, Offer, false, radioMode, title));
        }
    }
}
