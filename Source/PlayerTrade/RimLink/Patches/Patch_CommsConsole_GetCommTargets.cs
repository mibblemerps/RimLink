using System.Collections.Generic;
using HarmonyLib;
using RimLink.Core;
using RimWorld;

namespace RimLink.Patches
{
    /// <summary>
    /// Add players to the comms console.
    /// </summary>
    [HarmonyPatch(typeof(Building_CommsConsole), "GetCommTargets")]
    public class Patch_CommsConsole_GetCommTargets
    {
        private static void Postfix(ref IEnumerable<ICommunicable> __result)
        {
            if (!RimLinkMod.Active)
                return;

            var comms = new List<ICommunicable>();

            // Add default comms entries
            comms.AddRange(__result);

            // Add player traders
            foreach (Player player in RimLink.Instance.Client.GetPlayers())
                comms.Add(new PlayerComms(player));

            __result = comms;
        }
    }
}
