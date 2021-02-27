using System;
using System.Collections.Generic;
using System.Reflection;
using PlayerTrade.Net.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class NetHediff : IPacketable
    {
        public string HediffDefName;
        public int BodypartIndex;
        public string SourceDefName;
        public int AgeTicks;
        public float Severity;

        public List<NetHediffComp> Comps = new List<NetHediffComp>();

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteString(HediffDefName);
            buffer.WriteInt(BodypartIndex);
            buffer.WriteString(SourceDefName, true);
            buffer.WriteInt(AgeTicks);
            buffer.WriteDouble(Severity);

            buffer.WriteInt(Comps.Count);
            foreach (var comp in Comps)
            {
                buffer.WriteString(comp.GetType().FullName);
                buffer.WritePacketable(comp);
            }
        }

        public void Read(PacketBuffer buffer)
        {
            HediffDefName = buffer.ReadString();
            BodypartIndex = buffer.ReadInt();
            SourceDefName = buffer.ReadString(true);
            AgeTicks = buffer.ReadInt();
            Severity = (float) buffer.ReadDouble();

            int compsCount = buffer.ReadInt();
            Comps = new List<NetHediffComp>(compsCount);
            for (int i = 0; i < compsCount; i++)
            {
                Type compType = Assembly.GetExecutingAssembly().GetType(buffer.ReadString());
                NetHediffComp comp = (NetHediffComp) Activator.CreateInstance(compType);
                comp.Read(buffer);
                Comps.Add(comp);
            }
        }

        public abstract class NetHediffComp : IPacketable
        {
            public abstract void Write(PacketBuffer buffer);

            public abstract void Read(PacketBuffer buffer);
        }

        public class NetHediffComp_Disappears : NetHediffComp
        {
            public int TicksToDisappear;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteInt(TicksToDisappear);
            }

            public override void Read(PacketBuffer buffer)
            {
                TicksToDisappear = buffer.ReadInt();
            }
        }

        public class NetHediffComp_GetsPermanent : NetHediffComp
        {
            public bool IsPermanent;
            public float PermanentDamageThreshold;
            public PainCategory PermanentPainCategory;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteBoolean(IsPermanent);
                buffer.WriteFloat(PermanentDamageThreshold);
                buffer.WriteByte((byte) PermanentPainCategory);
            }

            public override void Read(PacketBuffer buffer)
            {
                IsPermanent = buffer.ReadBoolean();
                PermanentDamageThreshold = buffer.ReadFloat();
                PermanentPainCategory = (PainCategory) buffer.ReadByte();
            }
        }

        public class NetHediffComp_Immunizable : NetHediffComp
        {
            public float Immunity;
            public float ImmunityRandomFactor;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteFloat(Immunity);
                buffer.WriteFloat(ImmunityRandomFactor);
            }

            public override void Read(PacketBuffer buffer)
            {
                Immunity = buffer.ReadFloat();
                ImmunityRandomFactor = buffer.ReadFloat();
            }
        }

        public class NetHediffComp_TendDuration : NetHediffComp
        {
            public int TendTicksLeft;
            public float TendQuality;
            public float TotalTendQuality;

            public override void Write(PacketBuffer buffer)
            {
                buffer.WriteInt(TendTicksLeft);
                buffer.WriteFloat(TendQuality);
                buffer.WriteFloat(TotalTendQuality);
            }

            public override void Read(PacketBuffer buffer)
            {
                TendTicksLeft = buffer.ReadInt();
                TendQuality = buffer.ReadFloat();
                TotalTendQuality = buffer.ReadFloat();
            }
        }
    }
}