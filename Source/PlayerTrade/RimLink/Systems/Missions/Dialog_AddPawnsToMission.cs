using System.Collections.Generic;
using RimLink.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Missions
{
    public class Dialog_AddPawnsToMission : Window
    {
        public PlayerMissionDef MissionDef;
        public Player Player;

        private List<Pawn> _availablePawns;
        private HashSet<Pawn> _goingPawns = new HashSet<Pawn>();

        private Vector2 _scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(460f, 460f);

        public Dialog_AddPawnsToMission(PlayerMissionDef missionDef, Player player)
        {
            MissionDef = missionDef;
            Player = player;

            doCloseX = true;
            forcePause = true;

            _availablePawns = new List<Pawn>(MissionUtil.GetPawnsAvailableForMissions(MissionDef));
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, inRect.width, 35f), "Choose Colonists");
            Text.Font = GameFont.Small;
            y += 45f;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(0, y, inRect.width, 25f), "You must choose at least " + MissionDef.colonists.min + " " + (MissionDef.colonists.min == 1 ? "colonist" : "colonists"));
            GUI.color = Color.white;
            y += 35f;

            bool hitMax = _goingPawns.Count >= MissionDef.colonists.max;

            const int pawnRowHeight = 50;
            Rect viewRect = new Rect(0, 0, inRect.width - 16f, pawnRowHeight * _availablePawns.Count);
            _scrollPosition = GUI.BeginScrollView(new Rect(0, y, inRect.width, inRect.height - y - 45f), _scrollPosition, viewRect);
            int i = 0;
            foreach (Pawn pawn in _availablePawns)
            {
                Rect pawnRect = new Rect(0, i++ * pawnRowHeight, viewRect.width, pawnRowHeight);
                GUI.BeginGroup(pawnRect);
                pawnRect = pawnRect.AtZero();

                if (i % 2 == 1)
                    Widgets.DrawHighlight(pawnRect);

                // Checkbox
                bool check = _goingPawns.Contains(pawn);
                bool oldChecked = check;
                Rect checkboxRect = pawnRect.LeftPartPixels(20f).TopPartPixels(20f).CenteredOnYIn(pawnRect);
                checkboxRect.x += 5f;
                Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref check, disabled: hitMax && !check);
                if (check != oldChecked)
                {
                    if (check)
                        _goingPawns.Add(pawn);
                    else
                        _goingPawns.Remove(pawn);
                }
                float x = checkboxRect.xMax;

                // Portrait
                Widgets.DrawTextureFitted(new Rect(x, 0, pawnRowHeight, pawnRowHeight), PortraitsCache.Get(pawn, new Vector2(45f, 45f)), 1f);
                x += pawnRowHeight + 5f;

                // Pawn name
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, 0, inRect.width, pawnRowHeight), pawn.NameFullColored);
                Text.Anchor = TextAnchor.UpperLeft;

                // Info button
                Widgets.InfoCardButtonCentered(pawnRect.RightPartPixels(30f), pawn);

                GUI.EndGroup();
            }
            GUI.EndScrollView();

            Rect buttonsRect = inRect.BottomPartPixels(35f);

            bool validColonistCount = _goingPawns.Count >= MissionDef.colonists.min &&
                          _goingPawns.Count <= MissionDef.colonists.max;
            string count = MissionDef.colonists.max >= 9999
                ? _goingPawns.Count.ToString()
                : _goingPawns.Count + "/" + MissionDef.colonists.max;
            if (Widgets.ButtonText(buttonsRect.RightPartPixels(200f), $"Send Colonists ({count})", active: validColonistCount))
            {
                if (!validColonistCount)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox($"Please choose at least {MissionDef.colonists.min} colonists."));
                    return;
                }

                if (!MissionDef.ShowConfigDialog(Player, _goingPawns))
                {
                    // No config dialog to show - fallback to sending mission with some default. This probably(?) shouldn't be used
                    Log.Error("Mission missing a config dialog. Sending mission with some basic defaults instead.");
                    Messages.Message("This mission is incomplete.", MessageTypeDefOf.RejectInput, false);
                }

                Close(false);
            }

            if (Widgets.ButtonText(buttonsRect.LeftPartPixels(200f), "Close"))
            {
                Close();
            }

            GUI.EndGroup();
        }
    }
}
