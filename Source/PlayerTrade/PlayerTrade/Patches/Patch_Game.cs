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

            string ip = PlayerTradeMod.Instance.Settings.ServerIp;

            if (ip.NullOrEmpty())
            {
                Log.Message("Not connecting to trade server: No IP set");
                return;
            }

            if (!PlayerTradeMod.Instance.Connected)
            {
                // Connect
                Log.Message("Connecting to: " + ip);
                await PlayerTradeMod.Instance.Connect();
            }

            Log.Message("Player trade active");

            // Now tradable
            PlayerTradeMod.Instance.Client.IsTradableNow = true;
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
        }
    }
}
