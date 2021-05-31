using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(CameraDriver), "Update")]
    public static class Patch_CameraDriver_Update
    {
        public static List<SoundDef> PendingOneshots = new List<SoundDef>();

        private static void Prefix()
        {
            foreach (var pendingOneshot in PendingOneshots)
            {
                pendingOneshot.PlayOneShotOnCamera();
            }
            PendingOneshots.Clear();
        }
    }
}
