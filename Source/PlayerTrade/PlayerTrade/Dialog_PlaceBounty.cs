using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class Dialog_PlaceBounty : Window
    {
        public string Username;

        public override Vector2 InitialSize => new Vector2(512f, 1024f);

        public Dialog_PlaceBounty(string username)
        {
            Username = username;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.Label(inRect, "Work in progress...");
        }
    }
}
