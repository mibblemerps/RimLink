﻿using HarmonyLib;
using RimLink.Net;
using RimWorld;
using Verse;

namespace RimLink.Patches
{
    /// <summary>
    /// Prevents the user from disabling run in background when RimLink is active.
    /// This is because disabling run in background causes RimLink to disconnect.
    /// </summary>
    [HarmonyPatch(typeof(PrefsData), "Apply")]
    public class Patch_PrefsData_Apply
    {
        private static void Prefix(PrefsData __instance)
        {
            // Force run in background to true while mod is active
            if (RimLink.Instance != null && RimLink.Instance.Client != null &&
                RimLink.Instance.Client.State != Connection.ConnectionState.Disconnected)
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