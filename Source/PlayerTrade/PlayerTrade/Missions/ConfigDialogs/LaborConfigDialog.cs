using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Missions.MissionWorkers;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Missions.ConfigDialogs
{
    public class LaborConfigDialog : Window
    {
        public Player Player;
        public IEnumerable<Pawn> Pawns;

        private PlayerMissionDef _def;

        private int _silverAmount;
        private int _bondAmount;
        private float _daysToLend;

        private string _silverAmountBuffer;
        private string _bondAmountBuffer;
        private string _daysToLendBuffer;

        public override Vector2 InitialSize => new Vector2(480f, 330f);

        public LaborConfigDialog(PlayerMissionDef def, Player player, IEnumerable<Pawn> pawns)
        {
            Player = player;
            Pawns = pawns;
            _def = def;

            doCloseX = true;
            forcePause = true;
            closeOnAccept = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            float y = 0f;

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0, y, inRect.width, 50f);
            Widgets.Label(titleRect, $"Lend Colonist{(Pawns.Count() == 1 ? "" : "s")} to {Player.Name.Colorize(ColoredText.FactionColor_Neutral)}");
            Text.Font = GameFont.Small;
            y += titleRect.height + 10f;

            Rect silverAmountRect = new Rect(0, y, inRect.width, 30f);
            y += silverAmountRect.height + 5f;
            GUI.DrawTexture(silverAmountRect.LeftPartPixels(25f), ThingDefOf.Silver.uiIcon, ScaleMode.ScaleToFit);
            silverAmountRect.xMin += 25f;
            Widgets.Label(silverAmountRect.LeftPart(0.33f), "Silver");
            Widgets.TextFieldNumeric(silverAmountRect.RightPart(0.66f), ref _silverAmount, ref _silverAmountBuffer, -100000f, 100000f);

            Rect bondAmountRect = new Rect(0, y, inRect.width, 30f * 2f + 5f);
            y += bondAmountRect.height + 20f;
            bondAmountRect.xMin += 25f;
            Widgets.Label(bondAmountRect.TopHalf().LeftPart(0.33f), "Bond");
            Widgets.TextFieldNumeric(bondAmountRect.TopHalf().RightPart(0.66f), ref _bondAmount, ref _bondAmountBuffer, 0, 100000f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(bondAmountRect.BottomHalf(), "The bond is an amount paid by the other party that you will be paid if the lent colonist is significantly injured or killed.");
            Text.Font = GameFont.Small;

            Rect daysRect = new Rect(0, y, inRect.width, 30f);
            y += daysRect.height + 5f;
            Widgets.Label(daysRect.LeftPart(0.33f), "Days to lend");
            Widgets.TextFieldNumeric(daysRect.RightPart(0.66f), ref _daysToLend, ref _daysToLendBuffer, _def.days.min, _def.days.max);

            if (Widgets.ButtonText(inRect.BottomPartPixels(35f).RightPart(0.25f), "Lend Colonist"))
            {
                MissionUtil.SendMission(Player, _def, Pawns, new LaborMissionWorker
                {
                    Payment = _silverAmount,
                    Bond = _bondAmount
                }, _daysToLend);
                Messages.Message("Sent labor offer to " + Player.Name, MessageTypeDefOf.NeutralEvent, false);
                Close();
            }

            GUI.EndGroup();
        }
    }
}
