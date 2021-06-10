using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace RimLink.Patches
{
    /// <summary>
    /// Allows us to play queued up one-shot sounds. Allows playing one-shots from other threads.
    /// </summary>
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
