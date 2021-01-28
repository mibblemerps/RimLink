using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Building_CommsConsole), "GetCommTargets")]
    public class Patch_CommsConsole
    {
        private static void Postfix(ref IEnumerable<ICommunicable> __result)
        {
            if (!PlayerTradeMod.Connected)
                return;

            var comms = new List<ICommunicable>();

            // Add default comms entries
            comms.AddRange(__result);

            // Add player traders
            foreach (Player player in RimLinkComp.Instance.Client.GetPlayers())
                comms.Add(new PlayerComms(player));

            __result = comms;
        }
    }
}
