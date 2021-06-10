using System;
using System.Collections.Generic;
using RimLink.Util;
using RimLink.Core;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Missions
{
    public class Dialog_MissionSelect : Window
    {
        public Player Player;

        public override Vector2 InitialSize => new Vector2(600f, 580f);

        private List<PlayerMissionDef> _missionDefs;

        public Dialog_MissionSelect(Player player)
        {
            Player = player;

            doCloseX = true;
            doCloseButton = true;
            forcePause = true;

            _missionDefs = new List<PlayerMissionDef>(DefDatabase<PlayerMissionDef>.AllDefsListForReading);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, inRect.width, 35f), "Player Missions for " + Player.Guid.GuidToName(true));
            Text.Font = GameFont.Small;
            y += 45f;

            const int missionRowHeight = 45;
            int i = 0;
            foreach (PlayerMissionDef def in _missionDefs)
            {
                Rect defRect = new Rect(0, y, inRect.width, missionRowHeight);
                GUI.BeginGroup(defRect);
                defRect = defRect.AtZero();

                y += missionRowHeight;

                if (i % 2 == 0) // stripped highlight
                    Widgets.DrawHighlight(defRect);

                // Icon
                //Widgets.DrawTextureFitted(new Rect(0, 0, 35f, 35f), null, 0.9f);

                // Mission name
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(40f, 0, defRect.width - 35f, missionRowHeight), def.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;

                // Mission length and colonist count
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Rect missionInfoRect = defRect.RightPartPixels(100);
                missionInfoRect.x -= 130f;
                Widgets.Label(missionInfoRect, def.colonists.FormatIntRangeNice() + (def.colonists.max == 1 ? " colonist" : " colonists") + "\n" +
                                               def.days.FormatFloatRangeNice() + (Math.Abs(def.days.max - 1) < 0.01f ? " day" : " days"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;

                if (Widgets.ButtonText(defRect.RightPartPixels(120), "Start"))
                {
                    Find.WindowStack.Add(new Dialog_AddPawnsToMission(def, Player));
                    Close(false);
                }

                GUI.EndGroup();

                i++;
            }

            GUI.EndGroup();
        }
    }
}
