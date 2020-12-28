using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Net;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    public class Patch_ResourceCounter
    {
        public static Task SendPacketTask;

        private static void Postfix(ResourceCounter __instance)
        {
            if (SendPacketTask != null && !SendPacketTask.IsCompleted)
            {
                Log.Warn($"Getting held up sending colony resource counts. Skipping packets.");
                return;
            }

            if (!PlayerTradeMod.Instance.Connected)
                return;

            return; // todo: periodic resource updates obsolete

            var resources = new Resources();
            resources.Update(Find.CurrentMap);

            // Send packet to server of current resource count
            SendPacketTask = PlayerTradeMod.Instance.Client.SendPacket(new PacketColonyResources(
                PlayerTradeMod.Instance.Settings.Username, resources));
        }
    }
}
