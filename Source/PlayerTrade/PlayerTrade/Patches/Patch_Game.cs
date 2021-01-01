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
            RimLinkGameComponent.Find().Init();
        }
    }

    [HarmonyPatch(typeof(Game), "DeinitAndRemoveMap")]
    public static class Patch_Game_DeinitAndRemoveMap
    {
        public static void Prefix()
        {
            Log.Message("Game end");

            // Disconnect
            _ = PlayerTradeMod.Instance.Client.Disconnect();
            PlayerTradeMod.Instance.Client = null;
        }
    }
}
