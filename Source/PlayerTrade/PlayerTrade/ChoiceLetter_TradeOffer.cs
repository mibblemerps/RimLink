using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PlayerTrade
{
    public class ChoiceLetter_TradeOffer : ChoiceLetter
    {
        public TradeOffer Offer;

        public ChoiceLetter_TradeOffer(TradeOffer offer)
        {
            Offer = offer;

            def = DefDatabase<LetterDef>.GetNamed("PlayerTradeOffer");
            label = $"Trade Offer ({Offer.From})";
            text = offer.GetTradeOfferString(out var hyperlinks);
            hyperlinkThingDefs = hyperlinks.Take(Math.Min(hyperlinks.Count, 5)).ToList();
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                var accept = new DiaOption("Accept");
                accept.action = Accept;
                accept.resolveTree = true;
                if (!Offer.Fresh)
                    accept.Disable("Offer expired");
                if (!Offer.CanFulfill(true))
                    accept.Disable("Missing resources");
                yield return accept;

                var reject = new DiaOption("RejectLetter".Translate());
                reject.resolveTree = true;
                reject.action = Reject;
                yield return reject;

                yield return Option_Postpone;
            }
        }

        private void Accept()
        {
            if (!Offer.Fresh)
                return; // Offer not fresh (sanity check)

            Find.WindowStack.Add(new Dialog_TradeIntermission(Offer));

            _ = Offer.Accept();
        }

        private void Reject()
        {
            Option_Reject.action();
            if (Offer.Fresh)
            {
                _ = Offer.Reject(); // send rejection
                PlayerTradeMod.Instance.Client.ActiveTradeOffers.Remove(Offer);
            }

            Option_Close.action();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Offer, "offer");
        }
    }
}
