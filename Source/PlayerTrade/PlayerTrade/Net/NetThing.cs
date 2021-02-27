using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net.Packets;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace PlayerTrade.Net
{
    [Serializable]
    public class NetThing : IPacketable
    {
        public string ThingDef;
        public string StuffDef;
        public int StackCount;
        public int HitPoints;

        public float[] Color;

        public NetThing MinifiedInnerThing;

        public QualityCategory? Quality;

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
            // if (Color != null)
            // {
            //     buffer.WriteBoolean(true);
            //     buffer.WriteFloat(Color[0]);
            //     buffer.WriteFloat(Color[1]);
            //     buffer.WriteFloat(Color[2]);
            // }
            // else
            // {
            //     buffer.WriteBoolean(false);
            // }

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
            // if (buffer.ReadBoolean())
            //     Color = new []{ buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat() };

            // Read minified inner thing
            if (buffer.ReadBoolean())
            {
                MinifiedInnerThing = new NetThing();
                MinifiedInnerThing.Read(buffer);
            }

            // Read quality
            if (buffer.ReadBoolean())
                Quality = (QualityCategory) buffer.ReadInt();
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

            // var colorComp = thing.TryGetComp<CompColorable>();
            // if (colorComp != null && Color != null)
            //     colorComp.Color = Color.ToColor();

            if (thing is MinifiedThing minifiedThing)
                minifiedThing.InnerThing = MinifiedInnerThing.ToThing();

            if (Quality.HasValue)
            {
                var qualityComp = thing.TryGetComp<CompQuality>();
                qualityComp?.SetQuality(Quality.Value, ArtGenerationContext.Outsider);
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

            // var colorComp = thing.TryGetComp<CompColorable>();
            // if (colorComp != null && colorComp.Active)
            //     netThing.Color = colorComp.Color.ToFloats();

            if (thing is MinifiedThing minifiedThing)
                netThing.MinifiedInnerThing = FromThing(minifiedThing.InnerThing);

            if (thing.TryGetQuality(out QualityCategory quality))
                netThing.Quality = quality;

            return netThing;
        }

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
    }
}
