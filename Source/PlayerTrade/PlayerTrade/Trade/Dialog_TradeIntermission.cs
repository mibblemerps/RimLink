using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Trade
{
    [StaticConstructorOnStartup]
    public class Dialog_TradeIntermission : Window
    {
        public TradeOffer Offer;

        public override Vector2 InitialSize => new Vector2(412f, 128f);

        public Dialog_TradeIntermission(TradeOffer offer)
        {
            Offer = offer;

            forcePause = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;
            soundClose = SoundDefOf.ExecuteTrade;
            soundAmbient = SoundDefOf.RadioComms_Ambience;

            Offer.TradeAccepted.Task.ContinueWith((t) =>
            {
                if (IsOpen)
                    Close();
            });

            // Timeout after x seconds
            Task.Delay(5000).ContinueWith((t) =>
            {
                if (IsOpen)
                {
                    Close();
                    Messages.Message("Trade timed out", MessageTypeDefOf.NeutralEvent, false);
                }
            });
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            string text1 = "Confirming Trade...";
            Rect pos1 = inRect.TopPart(0.75f);
            GUI.Label(pos1, text1);

            string text2 = Offer.IsForUs ? Offer.From : Offer.For;
            Rect pos2 = inRect.BottomPart(0.25f);
            Text.Font = GameFont.Tiny;
            GUI.Label(pos2, text2);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
        }
    }
}
