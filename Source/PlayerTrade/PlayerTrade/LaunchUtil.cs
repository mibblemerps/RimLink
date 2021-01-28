using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class LaunchUtil
    {
        /// <summary>
        /// Launch thing into space!
        /// </summary>
        /// <param name="map">Map to find things on</param>
        /// <param name="launchThing">Thing to search for to launch. Factors such as hitpoints, quality, etc.. will be searched</param>
        /// <param name="count">Count to launch</param>
        /// <param name="dryRun"></param>
        /// <returns>Count launched</returns>
        public static int LaunchThing(Map map, Thing launchThing, int count, bool dryRun = false)
        {
            int launched = 0;

            if (launchThing is Pawn pawn)
            {
                if (!dryRun)
                    pawn.Destroy();
                return 1;
            }

            foreach (Building_OrbitalTradeBeacon orbitalTradeBeacon in Building_OrbitalTradeBeacon.AllPowered(map))
            {
                foreach (IntVec3 tradeableCell in orbitalTradeBeacon.TradeableCells)
                {
                    foreach (Thing thing in map.thingGrid.ThingsAt(tradeableCell))
                    {
                        if (IsThingSameAs(launchThing, thing))
                        {
                            int toLaunch = Mathf.Min(thing.stackCount, count - launched);
                            if (toLaunch <= 0)
                                continue; // depleted stack
                            if (!dryRun)
                                thing.SplitOff(toLaunch).Destroy(); // Remove amount

                            launched += toLaunch;
                            if (launched >= count)
                            {
                                if (launched > count)
                                    Log.Warn($"Launched more items than should have. (Launched {launchThing}, should've been {count}).");
                                return launched; // Launched count reached
                            }
                        }
                    }
                }
            }

            // Iterated over all things and didn't find enough to satisfy launch
            return launched;
        }

        /// <summary>
        /// Checks if 2 things are virtually the same thing.
        /// </summary>
        /// <param name="thingA">Thing A</param>
        /// <param name="thingB">Thing B</param>
        /// <param name="ignoreStackCount">Whether stack count is checked</param>
        /// <returns>Is same thing?</returns>
        public static bool IsThingSameAs(Thing thingA, Thing thingB, bool ignoreStackCount = true)
        {
            if (thingA is Pawn pawnA && thingB is Pawn pawnB)
            {
                return IsPawnSameAs(pawnA, pawnB);
            }

            if (thingA.def != thingB.def)
                return false;

            if (thingA.Stuff != thingB.Stuff)
                return false;

            if (thingA.HitPoints != thingB.HitPoints)
                return false;

            if (!ignoreStackCount && thingA.stackCount != thingB.stackCount)
                return false;

            if (thingA.TryGetQuality(out var qualityA) && thingB.TryGetQuality(out var qualityB))
            {
                if (qualityA != qualityB)
                    return false;
            }

            if (thingA is MinifiedThing minifiedA && thingB is MinifiedThing minifiedB)
            {
                if (minifiedA.InnerThing.def != minifiedB.InnerThing.def)
                    return false;
            }

            return true;
        }

        public static bool IsPawnSameAs(Pawn pawnA, Pawn pawnB)
        {
            return pawnA.ThingID == pawnB.ThingID
                   && Mathf.Abs(pawnA.MarketValue - pawnB.MarketValue) < 10f 
                   && Mathf.Abs(pawnA.ageTracker.AgeChronologicalTicks - pawnB.ageTracker.AgeChronologicalTicks) < 100;
        }

        public static int LaunchableThingCount(Map map, ThingDef def)
        {
            return TradeUtility.AllLaunchableThingsForTrade(map)
                .Where(t => t.def == def)
                .Sum((t => t.stackCount));
        }
    }
}
