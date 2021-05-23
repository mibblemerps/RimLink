using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PlayerTrade.Mechanoids.Designer;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(MechClusterUtility), "SpawnCluster")]
    public class Patch_MechClusterUtility
    {
        public static MechCluster Cluster;
    
        private static void Postfix(ref List<Thing> __result)
        {
            if (Cluster == null) return;
            
            List<Thing> things = new List<Thing>(__result);
            
            foreach (var partConfig in Cluster.Parts.Where(p => p.MechPart.Type == MechPart.PartType.Building))
            {
                foreach (ThingDef def in partConfig.GetThingDefs())
                {
                    Thing thing = things.FirstOrDefault(t => t.def == def);
                    if (thing != null)
                    {
                        // Remove from working list so we don't change settings on them again
                        things.Remove(thing);
                        
                        // Allow part config to configure Thing
                        partConfig.Configure(thing);
                    }
                }
            }

            Cluster = null;
        }
    }
}