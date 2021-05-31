using System;
using PlayerTrade.Net.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public static class PacketBufferExtensions
    {
        [Obsolete]
        public static void WriteThing(this PacketBuffer buffer, Thing thing)
        {
            // Write thingdef
            buffer.WriteString(thing.def.defName);

            // Write stuff (if applicable)
            if (thing.def.MadeFromStuff)
                buffer.WriteString(thing.Stuff.defName);

            // Write count
            buffer.WriteInt(thing.stackCount);

            // Write hitpoints
            buffer.WriteInt(thing.HitPoints);

            // Write minified content
            if (thing is MinifiedThing)
            {
                buffer.WriteBoolean(true);
                WriteThing(buffer, ((MinifiedThing) thing).InnerThing);
            }
            else
            {
                buffer.WriteBoolean(false);
            }

            // Write quality
            if (TryWriteComp<CompQuality>(thing, out var qualityComp))
            {
                buffer.WriteBoolean(true);
                buffer.WriteInt((int) qualityComp.Quality);
            }
            else
            {
                buffer.WriteBoolean(false);
            }
        }

        [Obsolete]
        public static Thing ReadThing(this PacketBuffer buffer)
        {
            ThingDef def = ThingDef.Named(buffer.ReadString());
            ThingDef stuff = null;
            if (def.MadeFromStuff) // read stuff
                stuff = ThingDef.Named(buffer.ReadString());

            Thing thing = ThingMaker.MakeThing(def, stuff);

            thing.stackCount = buffer.ReadInt();
            thing.HitPoints = buffer.ReadInt();

            // Read minified thing (if applicable)
            if (buffer.ReadBoolean())
            {
                Thing innerThing = ReadThing(buffer);
                ((MinifiedThing) thing).InnerThing = innerThing;
            }

            // Read quality (if applicable)
            if (buffer.ReadBoolean())
            {
                QualityCategory qualityCategory = (QualityCategory) buffer.ReadInt();
                var quality = thing.TryGetComp<CompQuality>();
                quality?.SetQuality(qualityCategory, ArtGenerationContext.Outsider);
            }

            return thing;
        }

        private static bool TryWriteComp<T>(Thing thing, out T comp) where T : ThingComp
        {
            comp = thing.TryGetComp<T>();
            return comp != null;
        }
    }
}
