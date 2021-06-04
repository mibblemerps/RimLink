using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PlayerTrade.Net.Packets;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace PlayerTrade.Net
{
    [Serializable]
    public class NetThing : IPacketable
    {
        private static FieldInfo CompBladelinkWeapon_Traits = typeof(CompBladelinkWeapon).GetField("traits", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo CompBladelinkWeapon_LastKillTick = typeof(CompBladelinkWeapon).GetField("lastKillTick", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo CompGeneratedNames_Name = typeof(CompGeneratedNames).GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public string ThingDef;
        public string StuffDef;
        public int StackCount;
        public int HitPoints;

        public float[] Color;

        public NetThing MinifiedInnerThing;

        public QualityCategory? Quality;

        public List<NetThingComp> Comps = new List<NetThingComp>();

        public void Write(PacketBuffer buffer)
        {
            // Write thingdef
            buffer.WriteString(ThingDef);

            // Write stuff
            if (StuffDef == null)
            {
                buffer.WriteBoolean(false);
            }
            else
            {
                buffer.WriteBoolean(true);
                buffer.WriteString(StuffDef);
            }
            
            buffer.WriteInt(StackCount);
            buffer.WriteInt(HitPoints);

            // Colors
            if (Color != null)
            {
                buffer.WriteBoolean(true);
                buffer.WriteFloat(Color[0]);
                buffer.WriteFloat(Color[1]);
                buffer.WriteFloat(Color[2]);
            }
            else
            {
                buffer.WriteBoolean(false);
            }

            // Write minified inner thing (if applicable)
            if (MinifiedInnerThing == null)
            {
                buffer.WriteBoolean(false);
            }
            else
            {
                buffer.WriteBoolean(true);
                MinifiedInnerThing.Write(buffer);
            }

            // Write quality
            if (Quality.HasValue)
            {
                buffer.WriteBoolean(true);
                buffer.WriteInt((int) Quality.Value);
            }
            else
            {
                buffer.WriteBoolean(false);
            }
            
            // Write comps
            buffer.WriteInt(Comps.Count);
            foreach (var comp in Comps)
            {
                buffer.WriteString(comp.GetType().FullName);
                buffer.WritePacketable(comp);
            }
        }

        public void Read(PacketBuffer buffer)
        {
            ThingDef = buffer.ReadString();

            // Read stuff
            if (buffer.ReadBoolean())
                StuffDef = buffer.ReadString();

            StackCount = buffer.ReadInt();
            HitPoints = buffer.ReadInt();

            // Colors
            if (buffer.ReadBoolean())
                Color = new []{ buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat() };

            // Read minified inner thing
            if (buffer.ReadBoolean())
            {
                MinifiedInnerThing = new NetThing();
                MinifiedInnerThing.Read(buffer);
            }

            // Read quality
            if (buffer.ReadBoolean())
                Quality = (QualityCategory) buffer.ReadInt();
            
            // Read comps
            int compCount = buffer.ReadInt();
            Comps = new List<NetThingComp>(compCount);
            for (int i = 0; i < compCount; i++)
            {
                Type compType = Assembly.GetExecutingAssembly().GetType(buffer.ReadString());
                NetThingComp comp = (NetThingComp) Activator.CreateInstance(compType);
                comp.Read(buffer);
                Comps.Add(comp);
            }
        }

        public Thing ToThing()
        {
            ThingDef thingDef = Verse.ThingDef.Named(ThingDef);
            ThingDef stuff = null;
            if (thingDef.MadeFromStuff)
                stuff = Verse.ThingDef.Named(StuffDef);

            Thing thing = ThingMaker.MakeThing(thingDef, stuff);

            thing.stackCount = StackCount;
            thing.HitPoints = HitPoints;

            var colorComp = thing.TryGetComp<CompColorable>();
            if (colorComp != null && Color != null)
                colorComp.Color = Color.ToColor();

            if (thing is MinifiedThing minifiedThing)
                minifiedThing.InnerThing = MinifiedInnerThing.ToThing();

            if (Quality.HasValue)
            {
                var qualityComp = thing.TryGetComp<CompQuality>();
                qualityComp?.SetQuality(Quality.Value, ArtGenerationContext.Outsider);
            }

            foreach (NetThingComp comp in Comps)
            {
                if (comp is NetThingComp_BladelinkWeapon netBladelinkComp)
                {
                    var bladelink = thing.TryGetComp<CompBladelinkWeapon>();
                    if (bladelink == null)
                    {
                        Log.Warn("Expected CompBladelinkWeapon");
                        continue;
                    }
                    
                    CompBladelinkWeapon_LastKillTick.SetValue(bladelink, Find.TickManager.TicksAbs - netBladelinkComp.LastKillTicksAgo);

                    // Load traits
                    var traitDefs = new List<WeaponTraitDef>();
                    foreach (string traitDefName in netBladelinkComp.Traits)
                        traitDefs.Add(DefDatabase<WeaponTraitDef>.GetNamed(traitDefName));
                    CompBladelinkWeapon_Traits.SetValue(bladelink, traitDefs);
                    
                    if (bladelink.TraitsListForReading == null) Log.Warn("Bladelink traits null");
                    
                    if (netBladelinkComp.Bonded)
                    {
                        // Load bonded pawn
                        Pawn pawn = PawnGuidThingComp.FindByGuid(netBladelinkComp.BondedPawnGuid);
                        if (pawn != null)
                        {
                            Log.Message($"Loading bond to {pawn} ({netBladelinkComp.BondedPawnGuid})...");
                            bladelink.Notify_Equipped(pawn);
                        }
                    }
                }
                else if (comp is NetThingComp_GeneratedNames netNameComp)
                {
                    CompGeneratedNames_Name.SetValue(thing.TryGetComp<CompGeneratedNames>(), netNameComp.Name);
                }
                else if (comp is NetThingComp_Ingredients netIngredients)
                {
                    var ingredients = thing.TryGetComp<CompIngredients>();
                    foreach (string ingredient in netIngredients.Ingredients)
                        ingredients.ingredients.Add(Verse.ThingDef.Named(ingredient));
                }
                else if (comp is NetThingComp_Rottable netRottable)
                {
                    thing.TryGetComp<CompRottable>().RotProgress = netRottable.RotProgress;
                }
            }

            return thing;
        }
        
        public static NetThing FromThing(Thing thing)
        {
            var netThing = new NetThing();
            netThing.ThingDef = thing.def.defName;
            if (thing.def.MadeFromStuff)
                netThing.StuffDef = thing.Stuff.defName;

            netThing.StackCount = thing.stackCount;
            netThing.HitPoints = thing.HitPoints;

            var colorComp = thing.TryGetComp<CompColorable>();
            if (colorComp != null && colorComp.Active)
                netThing.Color = colorComp.Color.ToFloats();

            if (thing is MinifiedThing minifiedThing)
                netThing.MinifiedInnerThing = FromThing(minifiedThing.InnerThing);

            if (thing.TryGetQuality(out QualityCategory quality))
                netThing.Quality = quality;

            // Bladelink
            var bladelinkComp = thing.TryGetComp<CompBladelinkWeapon>();
            if (bladelinkComp != null)
            {
                var bladelinkNetComp = new NetThingComp_BladelinkWeapon
                {
                    Bonded = bladelinkComp.Bondable,
                    LastKillTicksAgo = bladelinkComp.TicksSinceLastKill,
                    Traits = new List<string>()
                };
                if (bladelinkNetComp.Bonded)
                    bladelinkNetComp.BondedPawnGuid = bladelinkComp.bondedPawn?.TryGetComp<PawnGuidThingComp>()?.Guid;
                foreach (WeaponTraitDef trait in bladelinkComp.TraitsListForReading)
                    bladelinkNetComp.Traits.Add(trait.defName);
                netThing.Comps.Add(bladelinkNetComp);
            }

            // Generated name
            var generatedNames = thing.TryGetComp<CompGeneratedNames>();
            if (generatedNames != null)
            {
                netThing.Comps.Add(new NetThingComp_GeneratedNames
                {
                    Name = generatedNames.Name
                });
            }
            
            // Ingredients
            var ingredients = thing.TryGetComp<CompIngredients>();
            if (ingredients != null)
            {
                var ingredientsNet = new NetThingComp_Ingredients {Ingredients = new List<string>()};
                foreach (ThingDef ingredient in ingredients.ingredients)
                    ingredientsNet.Ingredients.Add(ingredient.defName);
                netThing.Comps.Add(ingredientsNet);
            }
            
            // Rottable
            var rottable = thing.TryGetComp<CompRottable>();
            if (rottable != null)
            {
                netThing.Comps.Add(new NetThingComp_Rottable {RotProgress = rottable.RotProgress});
            }

            return netThing;
        }

        public abstract class NetThingComp : IPacketable
        {
            public abstract void Write(PacketBuffer buffer);

            public abstract void Read(PacketBuffer buffer);
        }

        public class NetThingComp_Rottable : NetThingComp
        {
            public float RotProgress;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteFloat(RotProgress);
            }

            public override void Read(PacketBuffer buffer)
            {
                RotProgress = buffer.ReadFloat();
            }
        }
        
        public class NetThingComp_Ingredients : NetThingComp
        {
            public List<string> Ingredients = new List<string>();
            
            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteInt(Ingredients.Count);
                foreach (string ingredient in Ingredients)
                    buffer.WriteString(ingredient);
            }

            public override void Read(PacketBuffer buffer)
            {
                int count = buffer.ReadInt();
                Ingredients = new List<string>(count);
                for (int i = 0; i < count; i++)
                    Ingredients.Add(buffer.ReadString());
            }
        }

        public class NetThingComp_BladelinkWeapon : NetThingComp
        {
            public bool Bonded;
            public string BondedPawnGuid;
            public int LastKillTicksAgo;
            public List<string> Traits;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteBoolean(Bonded);
                buffer.WriteString(BondedPawnGuid, true);
                buffer.WriteInt(LastKillTicksAgo);
                
                buffer.WriteInt(Traits.Count);
                foreach (string trait in Traits)
                    buffer.WriteString(trait);
            }

            public override void Read(PacketBuffer buffer)
            {
                Bonded = buffer.ReadBoolean();
                BondedPawnGuid = buffer.ReadString(true);
                LastKillTicksAgo = buffer.ReadInt();
                
                int traitCount = buffer.ReadInt();
                Traits = new List<string>(traitCount);
                for (int i = 0; i < traitCount; i++)
                    Traits.Add(buffer.ReadString());
            }
        }

        public class NetThingComp_GeneratedNames : NetThingComp
        {
            public string Name;
            
            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteString(Name, true);
            }

            public override void Read(PacketBuffer buffer)
            {
                Name = buffer.ReadString(true);
            }
        }

        #region Debug

        [DebugAction("RimLink", "SendReceiveThing", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DebugSendReceiveThing()
        {
            Thing thing = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).FirstOrDefault(t => t.def.category == ThingCategory.Item);
            if (thing == null)
            {
                Log.Message("Nothing here");
                return;
            }

            TradeUtility.SpawnDropPod(UI.MouseCell(), Find.CurrentMap, FromThing(thing).ToThing());
            thing.Destroy();
        }
        
        #endregion
    }
}
