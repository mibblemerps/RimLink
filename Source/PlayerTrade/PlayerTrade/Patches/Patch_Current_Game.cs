using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Anticheat;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Current), "Game", MethodType.Setter)]
    public static class Patch_Current_Game
    {
        private static void Postfix()
        {
            if (RimLinkComp.Instance != null)
            {
                Log.Message("Game ended - shutdown RimLink.");
                // Disconnect
                try
                {
                    if (RimLinkComp.Instance.Client != null)
                        RimLinkComp.Instance.Client.Disconnect();
                    RimLinkComp.Instance.Client = null;
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to shutdown RimLink! " + e.Message);
                }

                AnticheatUtil.ShutdownAnticheat();

                RimLinkComp.Instance = null;
            }
        }
    }
}
