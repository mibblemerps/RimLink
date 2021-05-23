using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PlayerTrade.Mechanoids.Designer;
using RimWorld;
using RimWorld.SketchGen;
using UnityEngine;
using Verse;
// ReSharper disable RedundantAssignment

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(MechClusterGenerator), "GenerateClusterSketch_NewTemp")]
    public static class Patch_MechClusterGenerator
    {
        public static Map Map;
        public static MechCluster Cluster;

        private static MethodInfo _addBuildingsToSketch;
        private static SimpleCurve _pointsToSizeCurve;
        private static FloatRange _sizeRandomFactor;
        
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static void Init()
        {
            _pointsToSizeCurve = (SimpleCurve) typeof(MechClusterGenerator).GetField("PointsToSizeCurve",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _sizeRandomFactor = (FloatRange) typeof(MechClusterGenerator).GetField("SizeRandomFactorRange",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _addBuildingsToSketch = typeof(MechClusterGenerator).GetMethod("AddBuildingsToSketch",
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static bool Prefix(ref MechClusterSketch __result, float points, Map map, bool startDormant = true, bool forceNoConditionCauser = false)
        {
            if (Cluster == null)
            {
                // Skip patch, no mech cluster set to generate
                return true;
            }
            
            Log.Message("Overriding mech cluster generation...");

            try
            {
                if (_pointsToSizeCurve == null) Init();

                points = Cluster.CombatPower;

                // First we need to generate the building sketch. Normally this is done by calling to the MechCluster sketch resolver
                // We reimplement it here so we can custom generate the cluster
                Sketch buildingSketch = GenerateBuildingSketch(points);

                // Get mechs
                List<MechClusterSketch.Mech> mechs = new List<MechClusterSketch.Mech>();
                foreach (MechPartConfig part in Cluster.Parts.Where(part => part.MechPart.Type == MechPart.PartType.Pawn))
                {
                    mechs.Add(new MechClusterSketch.Mech(part.MechPart.PawnKindDef));
                }

                // Done
                __result = new MechClusterSketch(buildingSketch, mechs, Cluster.StartDormant);
                
                // Clear cluster data so we don't generate another one
                Cluster = null;
                Map = null;
                
                return false;
            }
            catch (Exception e)
            {
                Log.Error("Failed to generate predefined mech cluster!" , e);
                
                // Clear cluster data so the game doesn't try and generate this again.
                Cluster = null;
                Map = null;
                
                // Give blank sketch to hopefully generate a blank mech cluster
                __result = new MechClusterSketch(new Sketch(), new List<MechClusterSketch.Mech>(), false);
                return false;
            }
        }

        private static Sketch GenerateBuildingSketch(float points)
        {
            // The desired size based on the points
            int x = GenMath.RoundRandom(_pointsToSizeCurve.Evaluate(points) * _sizeRandomFactor.RandomInRange);
            int y = GenMath.RoundRandom(_pointsToSizeCurve.Evaluate(points) * _sizeRandomFactor.RandomInRange);
            
            // The largest rect we're able to find to place the cluster
            CellRect largestRect = LargestAreaFinder.FindLargestRect(Map,
                (cell => !cell.Impassable(Map) && cell.GetTerrain(Map).affordances.Contains(TerrainAffordanceDefOf.Heavy)),
                Mathf.Max(x, y));

            // Final size of cluster
            IntVec2 size = new IntVec2(Mathf.Min(x, largestRect.Width), Mathf.Min(y, largestRect.Height));

            Sketch sketch = new Sketch();
            
            // Optionally generate walls
            if (Cluster.HasWalls)
            {
                ResolveParams wallsParams = new ResolveParams {sketch = sketch, mechClusterSize = size};
                SketchResolverDefOf.MechClusterWalls.Resolve(wallsParams);
            }

            // Add buildings to sketch
            List<ThingDef> things = new List<ThingDef>();
            foreach (MechPartConfig part in Cluster.Parts.Where(part => part.MechPart.Type == MechPart.PartType.Building))
                things.AddRange(part.GetThingDefs());
            _addBuildingsToSketch.Invoke(null, new object[]{sketch, size, things});

            return sketch;
        }
    }
}