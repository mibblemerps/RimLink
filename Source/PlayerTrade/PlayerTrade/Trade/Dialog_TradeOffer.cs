using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Trade
{
    public class Dialog_TradeOffer : Dialog_NodeTreeWithFactionInfo
    {
        public TradeOffer Offer;

        private Vector2 _scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(780, 540);

        public Dialog_TradeOffer(DiaNode nodeRoot, TradeOffer offer, bool delayInteractivity = false, bool radioMode = false, string title = null) : base(nodeRoot, null, delayInteractivity, radioMode, title)
        {
            Offer = offer;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            
            Rect tradeItemsRect = new Rect(0, 30f, inRect.width, 320f);
            TradeOfferUI.DrawTradeItems(tradeItemsRect, ref _scrollPosition, Offer);
        }
    }
}
