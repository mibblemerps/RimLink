﻿using UnityEngine;
using Verse;

namespace RimLink
{
    public class Dialog_ConnectingToServer : Window
    {
        public override Vector2 InitialSize => new Vector2(328f, 64f);

        public Dialog_ConnectingToServer()
        {
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "Connecting to server...");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}
