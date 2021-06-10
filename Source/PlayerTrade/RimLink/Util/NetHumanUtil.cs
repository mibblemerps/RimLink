using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimLink.Core;
using RimLink.Net;
using RimWorld;
using Verse;

namespace RimLink.Util
{
    public static class NetHumanUtil
    {
        /// <summary>These thoughts are not sent over the network.</summary>
        public static List<string> NonNetworkedThoughtDefNames = new List<string>
        {
            "OnDuty",
        };
        
        private static FieldInfo GetsPermanent_PainCategory = typeof(HediffComp_GetsPermanent).GetField("painCategory", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo ImmunityHandler_TryAddImmunityRecord = typeof(ImmunityHandler).GetMethod("TryAddImmunityRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo HediffComp_Immunizable_SeverityPerDayNotImmuneRandomFactor = typeof(HediffComp_Immunizable).GetField("severityPerDayNotImmuneRandomFactor", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo HediffComp_TendDuration_TotalTendQuality = typeof(HediffComp_TendDuration).GetField("totalTendQuality", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo Pawn_RecordsTracker_Records = typeof(Pawn_RecordsTracker).GetField("records", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo InspirationHandler_CurState = typeof(InspirationHandler).GetField("curState", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo Need_CurLevel = typeof(Need).GetField("curLevelInt", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo PsychicEntropyTracker_LastMeditationTick = typeof(Pawn_PsychicEntropyTracker).GetField("lastMeditationTick",  BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo PsychicEntropyTracker_CurrentEntropy = typeof(Pawn_PsychicEntropyTracker).GetField("currentEntropy",  BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo PsychicEntropyTracker_CurrentPsyfocus = typeof(Pawn_PsychicEntropyTracker).GetField("currentPsyfocus",  BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo HediffComp_KillAfterDays_AddedTick = typeof(HediffComp_KillAfterDays).GetField("addedTick", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public static NetHuman ToNetHuman(this Pawn pawn, Mode mode = Mode.Full)
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

            PawnGuidComp guidComp = pawn.TryGetComp<PawnGuidComp>();
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

            if (mode == Mode.Simplified)
                return human; // All we need for simplified

            // Equipment
            human.Equipment = new List<NetThing>();
            if (mode == Mode.Full)
                foreach (ThingWithComps thing in pawn.equipment.AllEquipmentListForReading)
                    human.Equipment.Add(NetThing.FromThing(thing));

            // Apparel
            human.Apparel = new List<NetThing>();
            foreach (Apparel thing in pawn.apparel.WornApparel)
                human.Apparel.Add(NetThing.FromThing(thing));

            // Inventory
            human.Inventory = new List<NetThing>();
            if (mode == Mode.Full)
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

                    var killAfterDays = hediffWithComps.TryGetComp<HediffComp_KillAfterDays>();
                    if (killAfterDays != null)
                    {
                        netHediff.Comps.Add(new NetHediff.NetHediffComp_KillAfterDays
                        {
                            TicksAgo = Find.TickManager.TicksGame - (int) HediffComp_KillAfterDays_AddedTick.GetValue(killAfterDays)
                        });
                    }
                }

                // Save implant level (psylinks)
                if (hediff is Hediff_ImplantWithLevel implantWithLevel)
                    netHediff.ImplantLevel = implantWithLevel.level;

                human.Hediffs.Add(netHediff);
            }

            human.HealthState = pawn.health.State;

            // Work priorities
            human.WorkPriorities = new Dictionary<string, int>();
            if (mode == Mode.Full && pawn.workSettings.EverWork)
            {
                foreach (var workDef in DefDatabase<WorkTypeDef>.AllDefs)
                    human.WorkPriorities.Add(workDef.defName, pawn.workSettings.GetPriority(workDef));
            }

            // Records
            human.Records = new Dictionary<string, float>();
            if (mode == Mode.Full)
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
            if (mode == Mode.Full)
            {
                foreach (Need need in pawn.needs.AllNeeds)
                {
                    human.Needs.Add(new NetHuman.NetNeed
                    {
                        NeedDefName = need.def.defName,
                        Level = (float) Need_CurLevel.GetValue(need)
                    });
                }
            }

            // Memories
            human.Memories = new List<NetHuman.NetMemory>();
            if (mode == Mode.Full)
            {
                foreach (Thought_Memory memory in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (memory.otherPawn != null) continue; // Ignore memories that involve another pawn

                    if (NonNetworkedThoughtDefNames.Contains(memory.def.defName)) continue; // Not networked thought

                    human.Memories.Add(new NetHuman.NetMemory
                    {
                        ThoughtDefName = memory.def.defName,
                        Age = memory.age,
                        MoodPowerFactor = memory.moodPowerFactor,
                        Stage = memory.CurStageIndex
                    });
                }
            }

            // Abilities
            human.Abilities = new List<string>();
            foreach (Ability ability in pawn.abilities.abilities)
                human.Abilities.Add(ability.def.defName);

            // Royalty
            if (mode == Mode.Full && ModLister.RoyaltyInstalled && pawn.royalty != null)
            {
                var royalty = new NetRoyalty();
                human.Royalty = royalty;
                royalty.Favor = pawn.royalty.GetFavor(Faction.Empire);
                royalty.PermitPoints = pawn.royalty.GetPermitPoints(Faction.Empire);
                royalty.LastDecreeTicksAgo = Find.TickManager.TicksGame - pawn.royalty.lastDecreeTicks;
                royalty.AllowApparelRequirements = pawn.royalty.allowApparelRequirements;
                royalty.AllowRoomRequirements = pawn.royalty.allowRoomRequirements;

                // Titles
                foreach (var title in pawn.royalty.AllTitlesForReading)
                {
                    royalty.Titles.Add(new NetRoyalty.NetTitle
                    {
                        GotTicksAgo = Find.TickManager.TicksGame - title.receivedTick,
                        RoyalTitleDefName = title.def.defName,
                        Conceited = title.conceited,
                        WasInherited = title.wasInherited
                    });
                }
                
                // Permits
                foreach (var permit in pawn.royalty.PermitsFromFaction(Faction.Empire))
                {
                    royalty.Permits.Add(new NetRoyalty.NetPermit
                    {
                        UsedTicksAgo = Find.TickManager.TicksGame - permit.LastUsedTick,
                        PermitDefName = permit.Permit.defName,
                        TitleDefName = permit.Title.defName
                    });
                }

                // Heir
                Pawn heir = pawn.royalty.GetHeir(Faction.Empire);
                if (heir != null)
                {
                    // Create a simplified version of the heir. This heir will (hopefully) never been seen or spawned, other than their name in the UI.
                    royalty.DummyHeir = ToNetHuman(heir);
                    royalty.DummyHeir.Royalty = null; // Remove any royalty stuffs from heir (to prevent any loops and it's uneccesary data)
                }
                
                // Psychic
                royalty.HasPsylink = pawn.psychicEntropy != null;
                if (royalty.HasPsylink)
                {
                    royalty.CurrentEntropy = pawn.psychicEntropy.EntropyValue;
                    royalty.CurrentPsyfocus = pawn.psychicEntropy.CurrentPsyfocus;
                    royalty.TargetPsyfocus = pawn.psychicEntropy.TargetPsyfocus;
                    //royalty.LastMeditationTick = (int) PsychicEntropyTracker_LastMeditationTick.GetValue(pawn.psychicEntropy);
                    royalty.LimitEntropyAmount = pawn.psychicEntropy.limitEntropyAmount;
                }
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
            pawn.TryGetComp<PawnGuidComp>().Guid = human.RimLinkGuid;

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

            if (human.Apparel != null)
            {
                // Apparel
                pawn.apparel.DestroyAll();
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
            }

            if (human.Equipment != null)
            {
                // Equipment
                pawn.equipment.DestroyAllEquipment();
                foreach (NetThing netThing in human.Equipment)
                    pawn.equipment.AddEquipment((ThingWithComps) netThing.ToThing());
            }

            if (human.Inventory != null)
            {
                // Inventory
                pawn.inventory.innerContainer.ClearAndDestroyContents();
                foreach (NetThing thing in human.Inventory)
                    pawn.inventory.innerContainer.TryAdd(thing.ToThing());
            }

            // Hediffs
            if (usingBasePawn)
                pawn.health.hediffSet.Clear();
            foreach (NetHediff netHediff in human.Hediffs)
            {
                HediffDef def = HediffDef.Named(netHediff.HediffDefName);
                BodyPartRecord bodyPartRecord = null;
                if (netHediff.BodypartIndex >= 0)
                    bodyPartRecord = pawn.RaceProps.body.GetPartAtIndex(netHediff.BodypartIndex);

                Hediff hediff = HediffMaker.MakeHediff(def, pawn, bodyPartRecord);
                
                // Don't send letter that the pawn got a psylink when generating the pawn.
                if (hediff is Hediff_Psylink psylink)
                    psylink.suppressPostAddLetter = true;

                if (!string.IsNullOrWhiteSpace(netHediff.SourceDefName))
                    hediff.source = ThingDef.Named(netHediff.SourceDefName);
                hediff.Severity = netHediff.Severity;
                hediff.ageTicks = netHediff.AgeTicks;

                // Apply implant level (psylinks)
                if (hediff is Hediff_ImplantWithLevel implantWithLevel)
                    implantWithLevel.level = netHediff.ImplantLevel;

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
                        else if (netComp is NetHediff.NetHediffComp_KillAfterDays netKillAfterDays)
                        {
                            var killAfterDays = hediff.TryGetComp<HediffComp_KillAfterDays>();
                            HediffComp_KillAfterDays_AddedTick.SetValue(killAfterDays, Find.TickManager.TicksGame - netKillAfterDays.TicksAgo);
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
                
                // Add hediff to pawn
                pawn.health.AddHediff(hediff, bodyPartRecord);
            }

            // Work priorities
            foreach (var priority in human.WorkPriorities)
                pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed(priority.Key), priority.Value);

            // Records
            var records = (DefMap<RecordDef, float>) Pawn_RecordsTracker_Records.GetValue(pawn.records);
            if (human.Records != null)
            {
                foreach (var record in human.Records)
                    records[DefDatabase<RecordDef>.GetNamed(record.Key)] = record.Value;
            }

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
            
            // Memories
            foreach (NetHuman.NetMemory netMemory in human.Memories)
            {
                ThoughtDef def = DefDatabase<ThoughtDef>.GetNamed(netMemory.ThoughtDefName);
                Thought_Memory thought = (Thought_Memory) Activator.CreateInstance(def.ThoughtClass);
                thought.def = def;
                thought.age = netMemory.Age;
                thought.SetForcedStage(netMemory.Stage);
                thought.moodPowerFactor = netMemory.MoodPowerFactor;
                thought.Init();
                
                // Remove any thought that conflicts with this one
                var memoriesToRemove = new List<Thought_Memory>();
                foreach (Thought_Memory oldMemory in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (thought.GroupsWith(oldMemory)) // Conflicts with new memory, remove it
                        memoriesToRemove.Add(oldMemory);
                }
                foreach (Thought_Memory oldMemory in memoriesToRemove)
                    pawn.needs.mood.thoughts.memories.RemoveMemory(oldMemory);
                
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
            
            // Abilities
            if (human.Abilities != null)
            {
                foreach (string abilityDef in human.Abilities)
                    pawn.abilities.GainAbility(DefDatabase<AbilityDef>.GetNamed(abilityDef));
            }

            // Royalty
            if (ModLister.RoyaltyInstalled && human.Royalty != null)
            {
                if (pawn.royalty == null)
                    pawn.royalty = new Pawn_RoyaltyTracker(pawn);

                // Permit points
                var permitPointsDict = (Dictionary<Faction, int>) typeof(Pawn_RoyaltyTracker).GetField("permitPoints", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pawn.royalty);
                permitPointsDict[Faction.Empire] = human.Royalty.PermitPoints;

                // Add title
                NetRoyalty.NetTitle netTitle = human.Royalty.Titles.FirstOrDefault();
                if (netTitle != null)
                {
                    pawn.royalty.SetTitle(Faction.Empire, DefDatabase<RoyalTitleDef>.GetNamed(netTitle.RoyalTitleDefName), false, false, false);
                    var title = pawn.royalty.GetCurrentTitleInFaction(Faction.Empire);
                    title.conceited = netTitle.Conceited;
                    title.receivedTick = Find.TickManager.TicksGame - netTitle.GotTicksAgo;
                    title.wasInherited = netTitle.WasInherited;
                }

                pawn.royalty.SetFavor_NewTemp(Faction.Empire, human.Royalty.Favor); // (important this happens after adding the title, otherwise it gets cleared)
                pawn.royalty.lastDecreeTicks = Find.TickManager.TicksGame - human.Royalty.LastDecreeTicksAgo;
                pawn.royalty.allowRoomRequirements = human.Royalty.AllowRoomRequirements;
                pawn.royalty.allowApparelRequirements = human.Royalty.AllowApparelRequirements;

                if (!usingBasePawn && human.Royalty.DummyHeir != null)
                    pawn.royalty.SetHeir(human.Royalty.DummyHeir.ToPawn(), Faction.Empire);

                // Add permits
                foreach (var netPermit in human.Royalty.Permits)
                {
                    var permitDef = DefDatabase<RoyalTitlePermitDef>.GetNamed(netPermit.PermitDefName);
                    pawn.royalty.AddPermit(permitDef, Faction.Empire);
                    var permit = pawn.royalty.GetPermit(permitDef, Faction.Empire);
                    typeof(FactionPermit).GetField("lastUsedTick", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(permit, Find.TickManager.TicksGame - netPermit.UsedTicksAgo);
                }
                
                // Psychic
                if (human.Royalty.HasPsylink)
                {
                    if (pawn.psychicEntropy == null)
                        pawn.psychicEntropy = new Pawn_PsychicEntropyTracker(pawn);
                    
                    PsychicEntropyTracker_CurrentEntropy.SetValue(pawn.psychicEntropy, human.Royalty.CurrentEntropy);
                    PsychicEntropyTracker_CurrentPsyfocus.SetValue(pawn.psychicEntropy, human.Royalty.CurrentPsyfocus);
                    pawn.psychicEntropy.SetPsyfocusTarget(human.Royalty.TargetPsyfocus);
                    //PsychicEntropyTracker_LastMeditationTick.SetValue(pawn.psychicEntropy, human.Royalty.LastMeditationTick);
                    pawn.psychicEntropy.limitEntropyAmount = human.Royalty.LimitEntropyAmount;
                }
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

        public enum Mode
        {
            Full,
            Simplified,
            StartingColonist
        }
    }
}
