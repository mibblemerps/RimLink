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
            Log.Message("Game start");
            RimLinkComp.Find().Init();
        }
    }

    [HarmonyPatch(typeof(Game), "DeinitAndRemoveMap")]
    public static class Patch_Game_DeinitAndRemoveMap
    {
        public static void Prefix()
        {
            // todo: don't think this works - investigate using game comp dispose method instead
            Log.Message("Game end");

            // Disconnect
            _ = RimLinkComp.Find().Client.Disconnect();
            RimLinkComp.Find().Client = null;
        }
    }
}
