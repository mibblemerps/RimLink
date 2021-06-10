using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Net.Packets
{
    [Packet]
    public class PacketGiveItem : Packet
    {
        public string Reference;
        public string DefName;
        public string StuffDefName;
        public int Count;
        public float HealthPercentage;
        public QualityCategory? Quality;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Reference);
            buffer.WriteString(DefName);
            buffer.WriteInt(Count);
            buffer.WriteFloat(HealthPercentage);

            buffer.WriteBoolean(Quality.HasValue);
            if (Quality.HasValue)
                buffer.WriteByte((byte) Quality);
        }

        public override void Read(PacketBuffer buffer)
        {
            Reference = buffer.ReadString();
            DefName = buffer.ReadString();
            Count = buffer.ReadInt();
            HealthPercentage = buffer.ReadFloat();

            Quality = null;
            if (buffer.ReadBoolean())
                Quality = (QualityCategory) buffer.ReadByte();
        }

        public void GiveItem()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                throw new Exception("Player has no home maps!");

            ThingDef def = ThingDef.Named(DefName);
            if (def == null)
                throw new Exception($"ThingDef \"{DefName}\" not found!");

            ThingDef stuffDef = null;
            if (StuffDefName != null)
            {
                stuffDef = ThingDef.Named(StuffDefName);
                if (stuffDef == null)
                    throw new Exception($"Stuff ThingDef \"{StuffDefName}\" not found!");
            }

            Thing thing = ThingMaker.MakeThing(def, stuffDef);
            thing.stackCount = Count;
            thing.HitPoints = Mathf.RoundToInt(thing.MaxHitPoints * HealthPercentage);

            if (Quality.HasValue)
                thing.TryGetComp<CompQuality>()?.SetQuality(Quality.Value, ArtGenerationContext.Outsider);

            // Spawn item
            IntVec3 position = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
            TradeUtility.SpawnDropPod(position, map, thing);
            
            Log.Message($"Given item ({thing.Label}) from server.");
        }
    }
}
