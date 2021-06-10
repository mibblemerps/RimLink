using UnityEngine;
using Verse;

namespace RimLink.Systems.Trade
{
    public class Dialog_LoadingTradeWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(412f, 128f);

        public Dialog_LoadingTradeWindow()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;
            doWindowBackground = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            string text1 = "Loading Trade Things...";
            Widgets.Label(inRect, text1);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.EndGroup();
        }
    }
}