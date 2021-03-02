using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PlayerTrade.Labor
{
    public static class EscapeUtil
    {
        public static void Escaped(Pawn pawn)
        {
            LentColonistEscapeDef escapeDef = DefDatabase<LentColonistEscapeDef>.AllDefsListForReading.RandomElement();

            if (pawn.Spawned)
            {
                Log.Message("Already spawned pawn \"escaped\". Despawning...");
                pawn.DeSpawn();
            }

            if (escapeDef.damage)
            {
                EscapeRelatedDamage(pawn, escapeDef.damageMaxPartsToDamage, escapeDef.damageMaxDamagePercentagePerPart, escapeDef.damageBlunt);
            }

            if (escapeDef.tired)
            {
                pawn.needs.rest.CurLevelPercentage = 0f;
            }

            if (escapeDef.hungry)
            {
                pawn.needs.food.CurLevelPercentage = 0f;
                pawn.health.AddHediff(HediffDefOf.Malnutrition).Severity = Rand.Range(0.1f, 0.6f);
                
                // Remove any food from inventory
                var toRemove = new List<Thing>();
                foreach (Thing thing in pawn.inventory.innerContainer)
                {
                    if (thing.GetStatValue(StatDefOf.Nutrition) > 0.09f) // this value ensures only "proper" food counts (so not beer, for example)
                        toRemove.Add(thing);
                }
                foreach (Thing thing in toRemove)
                    thing.Destroy();
            }

            if (escapeDef.mugged)
            {
                pawn.inventory?.innerContainer?.ClearAndDestroyContents();
                pawn.apparel?.DestroyAll();
                pawn.equipment?.DestroyAllEquipment();
            }

            Map map = Find.AnyPlayerHomeMap;

            switch (escapeDef.arrivalMethod)
            {
                case LentColonistEscapeDef.ArrivalMethod.WalkIn:
                    if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 cell, map, CellFinder.EdgeRoadChance_Friendly, false))
                        goto case LentColonistEscapeDef.ArrivalMethod.DropPod; // use drop pods if we couldn't find an edge cell
                    GenPlace.TryPlaceThing(pawn, cell, map, ThingPlaceMode.Near);
                    pawn.Position = cell;
                    break;

                case LentColonistEscapeDef.ArrivalMethod.Shuttle:
                    if (!ModLister.RoyaltyInstalled)
                        goto case LentColonistEscapeDef.ArrivalMethod.DropPod; // fallback to drop pods if royalty isn't installed
                    MakeDropoffShuttle(map, pawn);
                    break;

                case LentColonistEscapeDef.ArrivalMethod.DropPod:
                    DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(map), map, new []{pawn}, leaveSlag: true, forbid: false);
                    break;
            }

            pawn.jobs?.StopAll();
            
            if (escapeDef.mentalState != null)
            {
                // Try to apply mental state - if it fails, try to apply the fallback mental state
                if (!pawn.mindState.mentalStateHandler.TryStartMentalState(escapeDef.mentalState, forceWake: true, transitionSilently: true) &&
                    escapeDef.fallbackMentalState != null)
                {
                    // Fallback mental state
                    pawn.mindState.mentalStateHandler.TryStartMentalState(escapeDef.fallbackMentalState,
                        forceWake: true, transitionSilently: true);
                }
            }

            LaborOffer offer = pawn.FindLaborOffer();
            if (offer == null)
                Log.Warn("Escaped pawn not associated with a labor offer!");
            string from = offer == null ? "Unknown" : offer.From;

            Find.LetterStack.ReceiveLetter("Returned Home: " + pawn.Name,
                escapeDef.reason.Replace("{pawn}", pawn.NameFullColored)
                    .Replace("{from}", from.GuidToName(true)), LetterDefOf.PositiveEvent,
                new LookTargets(pawn));
        }

        public static void EscapeRelatedDamage(Pawn pawn, int maxPartsToDamage = 3, float maxPercentDamagePerPart = 0.66f, bool blunt = false)
        {
            int partCountToDamage = Rand.RangeInclusive(1, maxPartsToDamage);
            var parts = new List<BodyPartRecord>();
            for (int i = 0; i < partCountToDamage; i++)
            {
                var limb = pawn.health.hediffSet
                    .GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside)
                    .FirstOrDefault(r => !parts.Contains(r) && !r.def.conceptual);

                if (limb == null)
                    break;

                parts.Add(limb);
            }

            if (parts.Count == 0)
            {
                Log.Warn("Couldn't find suitable parts to damage for pawn " + pawn.Name);
                return;
            }

            foreach (BodyPartRecord part in parts)
            {
                DamageDef damageDef = HealthUtility.RandomViolenceDamageType();

                float partMaxHealth = part.def.GetMaxHealth(pawn);
                // Amount of damage to apply
                float damage = Rand.Range(1, partMaxHealth * maxPercentDamagePerPart);
                // A safe amount of damage that won't destroy the body part and will be used if destroying the body part would kill the colonist.
                float damageSafe = Mathf.Min(damage, pawn.health.hediffSet.GetPartHealth(part) - 1f);
                HediffDef hediffDef = HealthUtility.GetHediffDefFromDamage(damageDef, pawn, part);

                // If we can take the full brunt, we do, if it'd down the colonist, we try and apply a "safe" damage amount instead
                if (IsSafeToApplyDamage(pawn, hediffDef, part, damage) && !blunt)
                {
                    Log.Message($"Full damage {damageDef.label} ({(part.Label)}): {damage}");
                    pawn.TakeDamage(new DamageInfo(damageDef, damage, 999f, hitPart: part));
                }
                else if (IsSafeToApplyDamage(pawn, HealthUtility.GetHediffDefFromDamage(DamageDefOf.Blunt, pawn, part), part, damage, maxBleedRate: 999f))
                {
                    Log.Message($"Blunt damage {damageDef.label} ({(part.Label)}): {damage}");
                    blunt = true;
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage, 999f, hitPart: part));
                }
                else if (damageSafe > 0f && IsSafeToApplyDamage(pawn, hediffDef, part, damageSafe))
                {
                    Log.Message($"Safe damage {damageDef.label} ({(part.Label)}): {damageSafe}");
                    pawn.TakeDamage(new DamageInfo(damageDef, damageSafe, 999f, hitPart: part));
                }
                else
                {
                    Log.Message($"Couldn't damage part {part.Label} on pawn {pawn.Name}. No safe damage appears possible.");
                }
            }
        }

        /// <summary>
        /// Is it safe to apply this damage to this pawn?
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="def">Hediff def (get from <see cref="HealthUtility.GetHediffDefFromDamage"/>)</param>
        /// <param name="part">Body part</param>
        /// <param name="damage">Amount of damage</param>
        /// <param name="allowDowned">Whether we're allowed to down the pawn</param>
        /// <param name="maxBleedRate">Maximum bleed rate allowed (in blood% per day)</param>
        /// <returns>Is safe</returns>
        public static bool IsSafeToApplyDamage(Pawn pawn, HediffDef def, BodyPartRecord part, float damage, bool allowDowned = false, float maxBleedRate = 4f)
        {
            Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
            hediff.Severity = damage;

            if (pawn.health.hediffSet.BleedRateTotal + hediff.BleedRate > maxBleedRate)
                return false;

            if (!allowDowned && pawn.health.WouldBeDownedAfterAddingHediff(hediff))
                return false;

            if (pawn.health.WouldDieAfterAddingHediff(hediff))
                return false;

            return true;
        }

        /// <summary>
        /// Make a drop off shuttle for a pawn. Doesn't error if they're a world pawn unlike the vanilla method (<see cref="SkyfallerUtility.MakeDropoffShuttle"/>).
        /// The pawn does however need to be despawned.
        /// </summary>
        public static void MakeDropoffShuttle(Map map, Pawn pawn)
        {
            if (pawn.Spawned)
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
            thing.TryGetComp<CompTransporter>().innerContainer.TryAddRangeOrTransfer(new []{pawn});
            GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, Gen.YieldSingle(thing)), result, map, ThingPlaceMode.Near);
        }

        [DebugAction("RimLink", "Escaped", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugEscaped(Pawn pawn)
        {
            Escaped(pawn);
        }

        [DebugAction("RimLink", "Escape Damage", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugEscapeDamage(Pawn pawn)
        {
            EscapeRelatedDamage(pawn);
        }
    }
}
