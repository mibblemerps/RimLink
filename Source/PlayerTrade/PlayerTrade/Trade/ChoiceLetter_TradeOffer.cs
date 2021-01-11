using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PlayerTrade.Trade
{
    public class ChoiceLetter_TradeOffer : ChoiceLetter
    {
        public TradeOffer Offer;

        public ChoiceLetter_TradeOffer(TradeOffer offer)
        {
            Offer = offer;

            def = DefDatabase<LetterDef>.GetNamed("PlayerTradeOffer");
            label = $"Trade Offer ({RimLinkComp.Find().Client.GetName(Offer.From)})";
            text = offer.GetTradeOfferString(out var hyperlinks);
            hyperlinkThingDefs = hyperlinks.Take(Math.Min(hyperlinks.Count, 5)).ToList();
        }

        public ChoiceLetter_TradeOffer() { }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
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

            _ = Offer.Accept();
        }

        private void Reject()
        {
            Find.LetterStack.RemoveLetter(this);
            if (Offer.Fresh)
            {
                _ = Offer.Reject(); // send rejection
                RimLinkComp.Find().Client.ActiveTradeOffers.Remove(Offer);
            }
        }
    }
}
