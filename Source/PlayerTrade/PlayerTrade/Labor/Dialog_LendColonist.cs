using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Labor
{
    public class Dialog_LendColonist : Window
    {
        public Player Player;

        private Pawn _selectedColonist;
        private int _silverAmount;
        private int _bondAmount;
        private float _daysToLend;

        private string _silverAmountBuffer;
        private string _bondAmountBuffer;
        private string _daysToLendBuffer;

        public static bool HasLendableColonist => GetLendablePawns().Any();

        public override Vector2 InitialSize => new Vector2(512f, 512f);

        public Dialog_LendColonist(Player player)
        {
            Player = player;

            doCloseX = true;
            forcePause = true;
            closeOnAccept = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            _selectedColonist = GetLendablePawns().FirstOrDefault();
            if (_selectedColonist == null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox("The colony has no pawns available to be lend.", "Close", buttonADestructive: true));
                Close(false);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0, 0, inRect.width, 50f);
            Widgets.Label(titleRect, $"Lend Colonist to {Player.Name.Colorize(ColoredText.FactionColor_Neutral)}");
            Text.Font = GameFont.Small;

            Rect selectColonistRect = new Rect(0, titleRect.yMax + 10f, inRect.width, 30f);

            Widgets.Label(selectColonistRect.LeftPart(0.33f), "Select Colonist");
            if (Widgets.ButtonText(selectColonistRect.RightPart(0.66f), _selectedColonist.Name.ToStringFull))
            {
                var options = new List<FloatMenuOption>();
                foreach (Pawn pawn in GetLendablePawns())
                    options.Add(new FloatMenuOption(pawn.NameFullColored, () => { _selectedColonist = pawn; }));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect silverAmountRect = new Rect(0, selectColonistRect.yMax + 10f, inRect.width, 30f);
            GUI.DrawTexture(silverAmountRect.LeftPartPixels(30f), ThingDefOf.Silver.uiIcon, ScaleMode.ScaleToFit);
            silverAmountRect.xMin += 35f;
            Widgets.Label(silverAmountRect.LeftPart(0.33f), "Silver");
            Widgets.TextFieldNumeric(silverAmountRect.RightPart(0.66f), ref _silverAmount, ref _silverAmountBuffer, -100000f, 100000f);

            Rect bondAmountRect = new Rect(0, silverAmountRect.yMax + 10f, inRect.width, 30f * 2f + 5f);
            bondAmountRect.xMin += 35f;
            Widgets.Label(bondAmountRect.TopHalf().LeftPart(0.33f), "Bond");
            Widgets.TextFieldNumeric(bondAmountRect.TopHalf().RightPart(0.66f), ref _bondAmount, ref _bondAmountBuffer, 0, 100000f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(bondAmountRect.BottomHalf(), "The bond is an amount paid by the other party that you will be paid if the lent colonist is significantly injured or killed.");
            Text.Font = GameFont.Small;

            Rect daysRect = new Rect(0, bondAmountRect.yMax + 10f, inRect.width, 30f);
            Widgets.Label(daysRect.LeftPart(0.33f), "Days to lend");
            Widgets.TextFieldNumeric(daysRect.RightPart(0.66f), ref _daysToLend, ref _daysToLendBuffer, 0.1f, 120f);

            if (Widgets.ButtonText(inRect.BottomPartPixels(35f).RightPart(0.25f), "Lend Colonist"))
            {
                LaborUtil.SendOffer(FormOffer());
                Messages.Message("Sent labor offer to " + Player.Name, MessageTypeDefOf.NeutralEvent, false);
                Close();
            }

            GUI.EndGroup();
        }

        private static IEnumerable<Pawn> GetLendablePawns()
        {
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                if (pawn.royalty.AllTitlesForReading.Count > 0)
                    continue;
                yield return pawn;
            }
        }

        private LaborOffer FormOffer()
        {
            var offer = new LaborOffer
            {
                Guid = Guid.NewGuid().ToString(),
                From = Player.Self().Guid,
                For = Player.Guid,
                Payment = _silverAmount,
                Bond = _bondAmount,
                Colonists = new List<Pawn>{_selectedColonist},
                Days = _daysToLend,
                Fresh = true,
            };
            offer.GenerateMarketValues();
            return offer;
        }
    }
}
