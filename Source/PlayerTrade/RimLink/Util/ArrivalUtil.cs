using System.Linq;
using RimWorld;
using Verse;

namespace RimLink.Util
{
    public class ArrivalUtil
    {
        /// <summary>
        /// <p>Have pawns "arrive" on the map.</p>
        /// <p><b>Note:</b> If shuttle is chosen, but royalty isn't installed, this method will automatically fallback to drop pods</p>
        /// </summary>
        /// <param name="map">Map to arrive onto</param>
        /// <param name="method">Arrival method to use</param>
        /// <param name="pawns">Pawns to arrive</param>
        public static void Arrive(Map map, Method method, params Pawn[] pawns)
        {
            switch (method)
            {
                case Method.WalkIn:
                    // Find spawn center
                    if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 center, map, CellFinder.EdgeRoadChance_Friendly, false))
                        goto case Method.DropPod; // use drop pods if we couldn't find an edge cell
                    Rot4 rotation = Rot4.FromAngleFlat((map.Center - center).AngleFlat);

                    // Place pawns near spawn center point
                    foreach (var pawn in pawns)
                    {
                        IntVec3 cell = CellFinder.RandomClosewalkCellNear(center, map, 8);
                        GenPlace.TryPlaceThing(pawn, cell, map, ThingPlaceMode.Near);
                        pawn.Position = cell;
                        pawn.Rotation = rotation;
                    }

                    break;

                case Method.DropPod:
                    // Find spawn center
                    IntVec3 dropPodCenter = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer, out _);
                    if (!dropPodCenter.IsValid)
                        dropPodCenter = DropCellFinder.TradeDropSpot(map);

                    // Drop in
                    DropPodUtility.DropThingsNear(dropPodCenter, map, pawns, forbid: false);

                    break;

                case Method.Shuttle:
                    if (!ModLister.RoyaltyInstalled)
                        goto case Method.DropPod; // fallback to drop pods if royalty isn't installed
                    MakeDropoffShuttle(map, pawns);
                    break;
            }
        }

        /// <summary>
        /// Make a drop off shuttle for a pawn. Doesn't error if they're a world pawn unlike the vanilla method (<see cref="SkyfallerUtility.MakeDropoffShuttle"/>).
        /// The pawn does however need to be despawned.
        /// </summary>
        private static void MakeDropoffShuttle(Map map, params Pawn[] pawns)
        {
            if (pawns.Any(pawn => pawn.Spawned))
            {
                Log.Error("Tried to make dropoff shuttle for spawned pawn");
                return;
            }

            if (!DropCellFinder.TryFindShipLandingArea(map, out IntVec3 result, out Thing blockingThing))
            {
                if (blockingThing != null)
                    Messages.Message("ShuttleBlocked".Translate("BlockedBy".Translate(blockingThing).CapitalizeFirst()), blockingThing, MessageTypeDefOf.NeutralEvent);
                result = DropCellFinder.TryFindSafeLandingSpotCloseToColony(map, ThingDefOf.Shuttle.Size);
            }
            Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle);
            thing.TryGetComp<CompShuttle>().dropEverythingOnArrival = true;
            thing.TryGetComp<CompTransporter>().innerContainer.TryAddRangeOrTransfer(pawns);
            GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, Gen.YieldSingle(thing)), result, map, ThingPlaceMode.Near);
        }

        public enum Method
        {
            WalkIn,
            Shuttle,
            DropPod,
        }
    }
}
