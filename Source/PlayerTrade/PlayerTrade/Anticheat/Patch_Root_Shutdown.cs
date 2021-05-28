using HarmonyLib;
using Verse;

namespace PlayerTrade.Anticheat
{
    /// <summary>
    /// Shutdown anticheat before game exits.
    /// </summary>
    [HarmonyPatch(typeof(Root), "Shutdown")]
    public class Patch_Root_Shutdown
    {
        public static void Prefix()
        {
            if (AnticheatUtil.IsEnabled)
                AnticheatUtil.ShutdownAnticheat();
        }
    }
}