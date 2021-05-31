using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PlayerTrade.Trade
{
    public class Dialog_ConfirmTrade : Window
    {
        public TradeOffer Offer;
        public Dialog_PlayerTrade TradeDialog;

        private Vector2 _scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(780, 540);

        public Dialog_ConfirmTrade(TradeOffer offer, Dialog_PlayerTrade dialog)
        {
            Offer = offer;
            TradeDialog = dialog;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            inRect = inRect.AtZero();

            Rect tradeItemsRect = inRect.TopPartPixels(inRect.height - 40f);
            TradeOfferUI.DrawTradeItems(tradeItemsRect, ref _scrollPosition, Offer);

            Rect buttonsRect = inRect.BottomPartPixels(35f);
            Rect confirmButtonRect = buttonsRect.RightPartPixels(180f);
            Rect cancelButtonRect = buttonsRect.RightPartPixels(180f);
            cancelButtonRect.x -= confirmButtonRect.width + 5f;
            if (Widgets.ButtonText(confirmButtonRect, "Confirm"))
                Confirm();
            if (Widgets.ButtonText(cancelButtonRect, "Cancel"))
                Cancel();

            GUI.EndGroup();
        }

        public override void OnAcceptKeyPressed()
        {
            Confirm();
        }

        public override void OnCancelKeyPressed()
        {
            Cancel();
        }

        private void Confirm()
        {
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();

            // Send trade deal off
            RimLinkComp.Instance.Get<TradeSystem>().SendOffer(Offer);

            Close(false);
            TradeDialog?.Close(false);
            TradeSession.deal.Reset();
        }

        private void Cancel()
        {
            Close();
        }
    }
}
