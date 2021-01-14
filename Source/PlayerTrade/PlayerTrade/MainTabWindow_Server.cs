using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class MainTabWindow_Server : MainTabWindow
    {
        public override Vector2 RequestedTabSize => new Vector2(450f, 390f);
        public override MainTabWindowAnchor Anchor => MainTabWindowAnchor.Right;

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            Widgets.ButtonText(inRect.TopPartPixels(30f).RightPart(0.33f), "Admin"); // todo: admin
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }
    }
}
