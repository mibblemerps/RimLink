using System;
using System.Collections.Generic;
using System.Reflection;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade
{
    public static class NetHumanUtil
    {
        private static FieldInfo GetsPermanent_PainCategory = typeof(HediffComp_GetsPermanent).GetField("painCategory", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo ImmunityHandler_TryAddImmunityRecord = typeof(ImmunityHandler).GetMethod("TryAddImmunityRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo HediffComp_Immunizable_SeverityPerDayNotImmuneRandomFactor = typeof(HediffComp_Immunizable).GetField("severityPerDayNotImmuneRandomFactor", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo HediffComp_TendDuration_TotalTendQuality = typeof(HediffComp_TendDuration).GetField("totalTendQuality", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo Pawn_RecordsTracker_Records = typeof(Pawn_RecordsTracker).GetField("records", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo InspirationHandler_CurState = typeof(InspirationHandler).GetField("curState", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo Need_CurLevel = typeof(Need).GetField("curLevelInt", BindingFlags.NonPublic | BindingFlags.Instance);

        public static NetHuman ToNetHuman(this Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
                throw new ArgumentException("Attempt to convert non-human to NetHuman", nameof(pawn));

            NetHuman human = new NetHuman();

            // Name
            if (pawn.Name is NameSingle nameSingle)
                human.Name = new[] {nameSingle.Name};
            else if (pawn.Name is NameTriple nameTriple)
                human.Name = new[] {nameTriple.First, nameTriple.Nick, nameTriple.Last};
            else
                throw new Exception("Unknown name type! " + pawn.Name.GetType().Name);

            PawnGuidThingComp guidComp = pawn.TryGetComp<PawnGuidThingComp>();
            if (guidComp == null)
            {
                Log.Error("Couldn't find RimLink GUID comp on pawn!");
                return null;
            }
            human.RimLinkGuid = guidComp.Guid;

            human.BiologicalAgeTicks = pawn.ageTracker.AgeBiologicalTicks;
            human.ChronologicalAgeTicks = pawn.ageTracker.AgeChronologicalTicks;
            human.KindDefName = pawn.kindDef.defName;
            human.Melanin = pawn.story.melanin;
            human.BodyTypeDefName = pawn.story.bodyType.defName;
            human.CrownType = pawn.story.crownType;
            human.HairColor = pawn.story.hairColor;
            human.HairDefName = pawn.story.hairDef.defName;
            if (pawn.story.childhood != null)
                human.Childhood = pawn.story.childhood.identifier;
            if (pawn.story.adulthood != null)
                human.Adulthood = pawn.story.adulthood.identifier;
            human.Gender = pawn.gender;
            
            // Skills
            human.Skills = new List<NetHuman.NetSkill>(pawn.skills.skills.Count);
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                human.Skills.Add(new NetHuman.NetSkill
                {
                    SkillDefName = skill.def.defName,
                    Level = skill.Level,
                    Xp = skill.xpSinceLastLevel,
                    XpSinceMidnight = skill.xpSinceMidnight,
                    Passion = skill.passion
                });
            }

            // Traits
            human.Traits = new List<NetHuman.NetTrait>();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                human.Traits.Add(new NetHuman.NetTrait
                {
                    TraitDefName = trait.def.defName,
                    Degree = trait.Degree
                });
            }

            // Equipment
            human.Equipment = new List<NetThing>();
            foreach (ThingWithComps thing in pawn.equipment.AllEquipmentListForReading)
                human.Equipment.Add(NetThing.FromThing(thing));

            // Apparel
            human.Apparel = new List<NetThing>();
            foreach (Apparel thing in pawn.apparel.WornApparel)
                human.Apparel.Add(NetThing.FromThing(thing));

            // Inventory
            human.Inventory = new List<NetThing>();
            foreach (Thing thing in pawn.inventory.innerContainer.InnerListForReading)
                human.Inventory.Add(NetThing.FromThing(thing));

            // Hediffs
            human.Hediffs = new List<NetHediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {

                var netHediff = new NetHediff
                {
                    HediffDefName = hediff.def.defName,
                    BodypartIndex = hediff.Part?.Index ?? -1,
                    AgeTicks = hediff.ageTicks,
                    Severity = hediff.Severity,
                    SourceDefName = hediff.source?.defName,
                };

                if (hediff is HediffWithComps hediffWithComps)
                {
                    var immunizable = hediffWithComps.TryGetComp<HediffComp_Immunizable>();
                    if (immunizable != null)
                    {
                        netHediff.Comps.Add(new NetHediff.NetHediffComp_Immunizable
                        {
                            Immunity = immunizable.Immunity,
                            ImmunityRandomFactor = (float) HediffComp_Immunizable_SeverityPerDayNotImmuneRandomFactor.GetValue(immunizable)
                        });
                    }

                    var getsPermanent = hediffWithComps.TryGetComp<HediffComp_GetsPermanent>();
                    if (getsPermanent != null)
                    {
                        netHediff.Comps.Add(new NetHediff.NetHediffComp_GetsPermanent
                        {
                            IsPermanent = getsPermanent.isPermanentInt,
                            PermanentPainCategory = getsPermanent.PainCategory,
                            PermanentDamageThreshold = getsPermanent.permanentDamageThreshold
                        });
                    }

                    var disappears = hediffWithComps.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                    {
                        netHediff.Comps.Add(new NetHediff.NetHediffComp_Disappears
                        {
                            TicksToDisappear = disappears.ticksToDisappear
                        });
                    }

                    var tendDuration = hediffWithComps.TryGetComp<HediffComp_TendDuration>();
                    if (tendDuration != null)
                    {
                        netHediff.Comps.Add(new NetHediff.NetHediffComp_TendDuration
                        {
                            TendQuality = tendDuration.tendQuality,
                            TendTicksLeft = tendDuration.tendTicksLeft,
                            TotalTendQuality = (float) HediffComp_TendDuration_TotalTendQuality.GetValue(tendDuration)
                        });
                    }
                }

                human.Hediffs.Add(netHediff);
            }

            human.HealthState = pawn.health.State;

            // Work priorities
            human.WorkPriorities = new Dictionary<string, int>();
            if (pawn.workSettings.EverWork)
            {
                foreach (var workDef in DefDatabase<WorkTypeDef>.AllDefs)
                    human.WorkPriorities.Add(workDef.defName, pawn.workSettings.GetPriority(workDef));
            }

            // Records
            human.Records = new Dictionary<string, float>();
            foreach (var recordDef in DefDatabase<RecordDef>.AllDefs)
                human.Records.Add(recordDef.defName, pawn.records.GetValue(recordDef));

            // Inspiration
            if (pawn.mindState.inspirationHandler.Inspired)
            {
                var currentInspiration = pawn.mindState.inspirationHandler.CurState;
                human.InspirationDefName = currentInspiration.def.defName;
                human.InspirationAge = currentInspiration.Age;
            }

            // Player settings
            human.MedicalCare = pawn.playerSettings.medCare;
            human.HostilityResponseMode = pawn.playerSettings.hostilityResponse;
            human.SelfTend = pawn.playerSettings.selfTend;

            // Schedule
            human.Schedule = new List<string>(24);
            for (int i = 0; i < 24; i++)
            {
                human.Schedule.Add(pawn.IsColonist
                    ? pawn.timetable.GetAssignment(i).defName
                    : TimeAssignmentDefOf.Anything.defName);
            }

            // Needs
            human.Needs = new List<NetHuman.NetNeed>();
            foreach (Need need in pawn.needs.AllNeeds)
            {
                human.Needs.Add(new NetHuman.NetNeed
                {
                    NeedDefName = need.def.defName,
                    Level = (float) Need_CurLevel.GetValue(need)
                });
            }

            return human;
        }

        public static Pawn ToPawn(this NetHuman human, Pawn basePawn = null)
        {
            Pawn pawn = basePawn;
            bool usingBasePawn = basePawn != null;
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(human.KindDefName);
            if (pawn == null)
            {
                // Create new pawn
                pawn = (Pawn) ThingMaker.MakeThing(kind.race);
                PawnComponentsUtility.CreateInitialComponents(pawn);
            }
            pawn.kindDef = kind;

            pawn.SetFaction(Faction.OfPlayer);

            // Set name
            if (human.Name.Length == 1)
                pawn.Name = new NameSingle(human.Name[0]);
            else
                pawn.Name = new NameTriple(human.Name[0], human.Name[1], human.Name[2]);

            // Set GUID
            pawn.TryGetComp<PawnGuidThingComp>().Guid = human.RimLinkGuid;

            // Backstory
            if (!BackstoryDatabase.TryGetWithIdentifier(human.Childhood, out pawn.story.childhood))
                throw new Exception("Failed to find backstory: " + human.Childhood);
            if (human.Adulthood != null && !BackstoryDatabase.TryGetWithIdentifier(human.Adulthood, out pawn.story.adulthood))
                throw new Exception("Failed to find backstory: " + human.Adulthood);

            pawn.ageTracker.AgeBiologicalTicks = human.BiologicalAgeTicks;
            pawn.ageTracker.AgeChronologicalTicks = human.ChronologicalAgeTicks;
            pawn.gender = human.Gender;
            pawn.story.bodyType = DefDatabase<BodyTypeDef>.GetNamed(human.BodyTypeDefName);
            pawn.story.melanin = human.Melanin;
            pawn.story.crownType = human.CrownType;
            pawn.story.hairDef = DefDatabase<HairDef>.GetNamed(human.HairDefName);
            pawn.story.hairColor = human.HairColor;

            // Traits
            if (usingBasePawn)
                pawn.story.traits.allTraits.Clear();
            foreach (NetHuman.NetTrait trait in human.Traits)
                pawn.story.traits.GainTrait(new Trait(trait.TraitDef, trait.Degree));

            // Skills
            foreach (NetHuman.NetSkill skill in human.Skills)
            {
                SkillRecord skillRecord = pawn.skills.GetSkill(skill.SkillDef);
                skillRecord.Level = skill.Level;
                skillRecord.passion = skill.Passion;
                skillRecord.xpSinceLastLevel = skill.Xp;
                skillRecord.xpSinceMidnight = skill.XpSinceMidnight;
            }
            pawn.workSettings.EnableAndInitialize();

            // Apparel
            foreach (NetThing netThing in human.Apparel)
            {
                Thing thing = netThing.ToThing();
                if (!(thing is Apparel))
                {
                    Log.Warn("Non apparel thing in received pawn's apparel: " + thing.LabelCap);
                    continue;
                }

                pawn.apparel.Wear((Apparel) thing, false); // todo: restore forced wearing
            }

            // Equipment
            foreach (NetThing netThing in human.Equipment)
                pawn.equipment.AddEquipment((ThingWithComps) netThing.ToThing());

            // Inventory
            if (usingBasePawn)
                pawn.inventory.innerContainer.ClearAndDestroyContents();
            foreach (NetThing thing in human.Inventory)
                pawn.inventory.innerContainer.TryAdd(thing.ToThing());

            // Hediffs
            if (usingBasePawn)
                pawn.health.hediffSet.Clear();
            foreach (NetHediff netHediff in human.Hediffs)
            {
                HediffDef def = HediffDef.Named(netHediff.HediffDefName);
                BodyPartRecord bodyPartRecord = null;
                if (netHediff.BodypartIndex >= 0)
                    bodyPartRecord = pawn.RaceProps.body.GetPartAtIndex(netHediff.BodypartIndex);

                Hediff hediff = pawn.health.AddHediff(def, bodyPartRecord);
                if (!string.IsNullOrWhiteSpace(netHediff.SourceDefName))
                    hediff.source = ThingDef.Named(netHediff.SourceDefName);
                hediff.Severity = netHediff.Severity;
                hediff.ageTicks = netHediff.AgeTicks;
                if (netHediff.SourceDefName != null)
                    hediff.source = ThingDef.Named(netHediff.SourceDefName);

                foreach (NetHediff.NetHediffComp netComp in netHediff.Comps)
                {
                    try
                    {
                        if (netComp is NetHediff.NetHediffComp_GetsPermanent netGetsPermanent)
                        {
                            var getsPermanent = hediff.TryGetComp<HediffComp_GetsPermanent>();
                            getsPermanent.isPermanentInt = netGetsPermanent.IsPermanent;
                            GetsPermanent_PainCategory.SetValue(getsPermanent, netGetsPermanent.PermanentPainCategory);
                            getsPermanent.permanentDamageThreshold = netGetsPermanent.PermanentDamageThreshold;
                        }
                        else if (netComp is NetHediff.NetHediffComp_Immunizable netImmunizable)
                        {
                            ImmunityHandler immunityHandler = pawn.health.immunity;
                            ImmunityHandler_TryAddImmunityRecord.Invoke(immunityHandler,
                                new object[] {def, hediff.source});
                            var record = immunityHandler.GetImmunityRecord(def);
                            record.immunity = netImmunizable.Immunity;

                            HediffComp_Immunizable_SeverityPerDayNotImmuneRandomFactor.SetValue(
                                hediff.TryGetComp<HediffComp_Immunizable>(), netImmunizable.ImmunityRandomFactor);
                        }
                        else if (netComp is NetHediff.NetHediffComp_Disappears netDisappears)
                        {
                            var disappears = hediff.TryGetComp<HediffComp_Disappears>();
                            disappears.ticksToDisappear = netDisappears.TicksToDisappear;
                        }
                        else if (netComp is NetHediff.NetHediffComp_TendDuration netTendDuration)
                        {
                            var tendDuration = hediff.TryGetComp<HediffComp_TendDuration>();
                            tendDuration.tendQuality = netTendDuration.TendQuality;
                            tendDuration.tendTicksLeft = netTendDuration.TendTicksLeft;
                            HediffComp_TendDuration_TotalTendQuality.SetValue(tendDuration, netTendDuration.TotalTendQuality);
                        }
                        else
                        {
                            Log.Warn("Unknown net hediff comp! Type = " + netComp.GetType().FullName);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Exception applying net hediff comp (Type = {netComp.GetType().FullName})", e);
                    }
                }
            }

            // Work priorities
            foreach (var priority in human.WorkPriorities)
                pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed(priority.Key), priority.Value);

            // Records
            var records = (DefMap<RecordDef, float>) Pawn_RecordsTracker_Records.GetValue(pawn.records);
            foreach (var record in human.Records)
                records[DefDatabase<RecordDef>.GetNamed(record.Key)] = record.Value;

            // Inspiration
            if (human.InspirationDefName != null)
            {
                Inspiration inspiration = new Inspiration
                {
                    def = DefDatabase<InspirationDef>.GetNamed(human.InspirationDefName),
                    pawn = pawn,
                    reason = human.InspirationReason
                };
                InspirationHandler_CurState.SetValue(pawn.mindState.inspirationHandler, inspiration);
            }

            // Player settings
            pawn.playerSettings.medCare = human.MedicalCare;
            pawn.playerSettings.hostilityResponse = human.HostilityResponseMode;
            pawn.playerSettings.selfTend = human.SelfTend;

            // Schedule
            for (int i = 0; i < 24; i++)
                pawn.timetable.SetAssignment(i, DefDatabase<TimeAssignmentDef>.GetNamed(human.Schedule[i]));

            // Needs
            foreach (NetHuman.NetNeed netNeed in human.Needs)
            {
                NeedDef def = DefDatabase<NeedDef>.GetNamed(netNeed.NeedDefName);
                Need need = pawn.needs.TryGetNeed(def);
                if (need == null)
                    Log.Warn($"Unable to set need {netNeed.NeedDefName} - need couldn't be found on pawn");
                Need_CurLevel.SetValue(need, netNeed.Level);
            }

            return pawn;
        }

        [DebugAction("RimLink", "SendReceivePawn", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugSendReceivePawn(Pawn pawn)
        {
            NetHuman human = pawn.ToNetHuman();
            pawn.Destroy();

            TradeUtility.SpawnDropPod(UI.MouseCell(), Find.CurrentMap, human.ToPawn());
        }
    }
}
