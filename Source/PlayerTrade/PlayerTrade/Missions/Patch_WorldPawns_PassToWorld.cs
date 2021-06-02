﻿using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace PlayerTrade.Missions
{
    [HarmonyPatch(typeof(WorldPawns), "PassToWorld")]
    public class Patch_WorldPawns_PassToWorld
    {
        private static bool Prefix(Pawn pawn)
        {
            if (pawn.TryGetComp<LentColonistComp>().GoneHome)
            {
                Log.Message($"Prevented {pawn} from becoming a world pawn (they've gone home)");
                return false;
            }

            return true;
        }
    }
}