using System.Collections.Generic;
using System.Linq;
using RimLink.Util;
using RimLink.Core;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimLink.Systems.Missions.ConfigDialogs
{
    public class BasicConfigDialog : Window
    {
        public Player Player;
        public IEnumerable<Pawn> Pawns;

        private PlayerMissionDef _def;
        private int _days;
        private string _daysBuffer;

        public override Vector2 InitialSize => new Vector2(440f, 210f);

        public BasicConfigDialog(PlayerMissionDef def, Player player, IEnumerable<Pawn> pawns)
        {
            Player = player;
            Pawns = pawns;
            _def = def;

            doCloseX = true;
            forcePause = true;
            closeOnAccept = false;

            _daysBuffer = Mathf.RoundToInt(_def.days.Average).ToString();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            float y = 0;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, inRect.width, 30), _def.LabelCap);
            y += 30;
            Text.Font = GameFont.Small;

            Rect daysRect = new Rect(0, y, inRect.width, 30f);
            y += daysRect.height + 2f;
            Widgets.Label(daysRect.LeftPart(0.5f), "Mission length (days)");
            Widgets.TextFieldNumeric(daysRect.RightPart(0.5f), ref _days, ref _daysBuffer, _def.days.min, _def.days.max);
            
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = Color.gray;

            Widgets.Label(new Rect(0, y, inRect.width, 20f).RightPart(0.5f), _def.days.FormatFloatRangeNice() + " days");
            y += 30f;

            Widgets.Label(new Rect(0, y, inRect.width, 20f), 
                $"After {_days} day{_days.MaybeS()}, {Player.Guid.GuidToName(true)} will return your colonist{Pawns.Count().MaybeS()}.");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            y += 20f;

            Rect bottomRect = inRect.BottomPartPixels(30f);

            if (Widgets.ButtonText(bottomRect.LeftPartPixels(180f), "Close"))
            {
                Close();
            }

            if (Widgets.ButtonText(bottomRect.RightPartPixels(180f), "Send"))
            {
                MissionUtil.SendMission(Player, _def, Pawns, null, _days);
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                Close(false);
            }

            GUI.EndGroup();
        }
    }
}
