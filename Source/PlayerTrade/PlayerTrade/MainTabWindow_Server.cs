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

            if (Widgets.ButtonText(inRect.TopPartPixels(30f).RightPart(0.33f), "Reinit"))
            {
                try
                {
                    RimLinkComp.Instance.Client.Tcp.Close();
                }
                catch (Exception) {}

                RimLinkComp.Instance.Init();
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
        }
    }
}
