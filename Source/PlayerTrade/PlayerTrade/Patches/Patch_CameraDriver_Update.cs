using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
