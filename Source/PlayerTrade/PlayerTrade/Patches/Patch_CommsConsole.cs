using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Building_CommsConsole), "GetCommTargets")]
    public class Patch_CommsConsole
    {
        private static void Postfix(ref IEnumerable<ICommunicable> __result)
        {
            if (!PlayerTradeMod.Instance.Connected)
                return;

            var comms = new List<ICommunicable>();

            // Add default comms entries
            comms.AddRange(__result);

            // Add player traders
            foreach (string username in RimLinkComp.Find().Client.TradablePlayers)
            {
                comms.Add(new PlayerComms(username));
            }

            __result = comms;
        }
    }
}
