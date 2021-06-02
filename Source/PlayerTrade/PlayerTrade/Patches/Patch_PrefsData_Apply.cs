using HarmonyLib;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(PrefsData), "Apply")]
    public class Patch_PrefsData_Apply
    {
        private static void Prefix(PrefsData __instance)
        {
            // Force run in background to true while mod is active
            if (RimLinkComp.Instance != null && RimLinkComp.Instance.Client != null &&
                RimLinkComp.Instance.Client.State != Connection.ConnectionState.Disconnected)
            {
                if (!__instance.runInBackground)
                {
                    // Notify user
                    Messages.Message("Rl_MessageRunInBackgroundDisabled".Translate(), MessageTypeDefOf.RejectInput);
                }
                __instance.runInBackground = true;
            }
        }
    }
}