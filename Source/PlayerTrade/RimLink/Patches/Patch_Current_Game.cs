using System;
using HarmonyLib;
using RimLink.Anticheat;
using RimLink.Net;
using Verse;

namespace RimLink.Patches
{
    /// <summary>
    /// Allows RimLink to see when the current game is shutdown (Current.Game = null). This allows us to shutdown RimLink.
    /// </summary>
    [HarmonyPatch(typeof(Current), "Game", MethodType.Setter)]
    public static class Patch_Current_Game
    {
        private static void Postfix()
        {
            if (RimLink.Instance != null)
            {
                Log.Message("Game ended - shutdown RimLink.");
                // Disconnect
                try
                {
                    if (RimLink.Instance.Client != null)
                        RimLink.Instance.Client.Disconnect(DisconnectReason.User);
                    RimLink.Instance.Client = null;
                }
                catch (Exception e)
                {
                    Log.Warn("Failed to shutdown RimLink! " + e.Message);
                }

                AnticheatUtil.ShutdownAnticheat();

                RimLink.Instance = null;
            }
        }
    }
}
