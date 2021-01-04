using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Net;
using UnityEngine;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public static class Patch_Game_FinalizeInit
    {
        public static async void Postfix()
        {
            // RimLinkComp.Find().Init();
        }
    }
}
