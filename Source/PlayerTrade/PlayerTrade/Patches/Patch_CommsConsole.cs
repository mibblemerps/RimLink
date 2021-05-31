using System.Collections.Generic;
using HarmonyLib;
using RimWorld;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Building_CommsConsole), "GetCommTargets")]
    public class Patch_CommsConsole
    {
        private static void Postfix(ref IEnumerable<ICommunicable> __result)
        {
            if (!RimLinkMod.Active)
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
