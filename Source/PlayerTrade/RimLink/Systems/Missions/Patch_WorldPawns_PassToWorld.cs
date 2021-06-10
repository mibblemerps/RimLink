using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RimLink.Systems.Missions
{
    [HarmonyPatch(typeof(WorldPawns), "PassToWorld")]
    public class Patch_WorldPawns_PassToWorld
    {
        private static bool Prefix(Pawn pawn)
        {
            var comp = pawn.TryGetComp<LentColonistComp>();
            if (comp != null && comp.GoneHome)
            {
                Log.Message($"Prevented {pawn} from becoming a world pawn (they've gone home)");
                return false;
            }

            return true;
        }
    }
}