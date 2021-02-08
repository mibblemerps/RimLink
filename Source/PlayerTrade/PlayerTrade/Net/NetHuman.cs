using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Net
{
    public partial class NetHuman : IPacketable
    {
        public string[] Name;
        public string RimLinkGuid;
        public long BiologicalAgeTicks;
        public long ChronologicalAgeTicks;
        public string KindDefName;
        public float Melanin;
        public Color SkinColor; // not sure how this differs to melanin but we include it
        public string BodyTypeDefName;
        public CrownType CrownType;
        public Color HairColor;
        public string HairDefName;
        public string Childhood;
        public string Adulthood;
        public Gender Gender;
        public List<NetSkill> Skills;
        public List<NetTrait> Traits;

        public List<NetThing> Equipment;
        public List<NetThing> Apparel;
        public List<NetThing> Inventory;
        public List<NetHediff> Hediffs;
        public PawnHealthState HealthState;

        public Dictionary<string, int> WorkPriorities;
        public Dictionary<string, float> Records;

        public string InspirationDefName;
        public int InspirationAge;
        public string InspirationReason;

        public MedicalCareCategory MedicalCare;
        public HostilityResponseMode HostilityResponseMode;
        public bool SelfTend;

        public List<string> Schedule;

        public List<NetNeed> Needs;

        public NetRoyalty Royalty;

        public NetHuman()
        {
            
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteString(RimLinkGuid);

            // Name
            buffer.WriteInt(Name.Length);
            foreach (string name in Name)
                buffer.WriteString(name);

            buffer.WriteLong(ChronologicalAgeTicks);
            buffer.WriteLong(BiologicalAgeTicks);
            buffer.WriteString(KindDefName);
            buffer.WriteDouble(Melanin);
            buffer.Write(SkinColor.ToFloats());
            buffer.WriteString(BodyTypeDefName);
            buffer.WriteByte((byte) CrownType);
            buffer.Write(HairColor.ToFloats());
            buffer.WriteString(HairDefName);
            buffer.WriteString(Childhood, true);
            buffer.WriteString(Adulthood, true);
            buffer.WriteByte((byte) Gender);
            
            // Skills
            buffer.WriteInt(Skills.Count);
            foreach (NetSkill skill in Skills)
                buffer.WritePacketable(skill);

            // Traits
            buffer.WriteInt(Traits.Count);
            foreach (NetTrait trait in Traits)
                buffer.WritePacketable(trait);

            // Equipment
            buffer.WriteInt(Equipment.Count);
            foreach (NetThing thing in Equipment)
                buffer.WritePacketable(thing);

            // Apparel
            buffer.WriteInt(Apparel.Count);
            foreach (NetThing thing in Apparel)
                buffer.WritePacketable(thing);

            // Inventory
            buffer.WriteInt(Inventory.Count);
            foreach (NetThing thing in Inventory)
                buffer.WritePacketable(thing);

            // Hediffs
            buffer.WriteInt(Hediffs.Count);
            foreach (NetHediff hediff in Hediffs)
                buffer.WritePacketable(hediff);

            buffer.WriteByte((byte) HealthState);

            // Work priorities
            buffer.WriteInt(WorkPriorities.Count);
            foreach (var priority in WorkPriorities)
            {
                buffer.WriteString(priority.Key);
                buffer.WriteInt(priority.Value);
            }

            // Records
            buffer.WriteInt(Records.Count);
            foreach (var record in Records)
            {
                buffer.WriteString(record.Key);
                buffer.WriteFloat(record.Value);
            }

            // Inspiration
            buffer.WriteString(InspirationDefName, true);
            buffer.WriteInt(InspirationAge);
            buffer.WriteString(InspirationReason, true);

            // Player settings
            buffer.WriteByte((byte) MedicalCare);
            buffer.WriteByte((byte) HostilityResponseMode);
            buffer.WriteBoolean(SelfTend);

            // Schedule
            for (int i = 0; i < 24; i++)
                buffer.WriteString(Schedule[i]);

            // Needs
            buffer.WriteInt(Needs.Count);
            foreach (NetNeed need in Needs)
                buffer.WritePacketable(need);

            // Royalty
            if (Royalty == null)
            {
                buffer.WriteBoolean(false);
            }
            else
            {
                buffer.WriteBoolean(true);
                buffer.WritePacketable(Royalty);
            }
        }

        public void Read(PacketBuffer buffer)
        {
            RimLinkGuid = buffer.ReadString();

            // Name
            int nameCount = buffer.ReadInt();
            Name = new string[nameCount];
            for (int i = 0; i < nameCount; i++)
                Name[i] = buffer.ReadString();

            ChronologicalAgeTicks = buffer.ReadLong();
            BiologicalAgeTicks = buffer.ReadLong();
            KindDefName = buffer.ReadString();
            Melanin = (float) buffer.ReadDouble();
            SkinColor = buffer.Read<float[]>().ToColor();
            BodyTypeDefName = buffer.ReadString();
            CrownType = (CrownType) buffer.ReadByte();
            HairColor = buffer.Read<float[]>().ToColor();
            HairDefName = buffer.ReadString();
            Childhood = buffer.ReadString(true);
            Adulthood = buffer.ReadString(true);
            Gender = (Gender) buffer.ReadByte();

            // Skills
            int skillCount = buffer.ReadInt();
            Skills = new List<NetSkill>(skillCount);
            for (int i = 0; i < skillCount; i++)
                Skills.Add(buffer.ReadPacketable<NetSkill>());

            // Traits
            int traitsCount = buffer.ReadInt();
            Traits = new List<NetTrait>(traitsCount);
            for (int i = 0; i < traitsCount; i++)
                Traits.Add(buffer.ReadPacketable<NetTrait>());

            // Equipment
            int equipmentCount = buffer.ReadInt();
            Equipment = new List<NetThing>(equipmentCount);
            for (int i = 0; i < equipmentCount; i++)
                Equipment.Add(buffer.ReadPacketable<NetThing>());

            // Apparel
            int apparelCount = buffer.ReadInt();
            Apparel = new List<NetThing>(apparelCount);
            for (int i = 0; i < apparelCount; i++)
                Apparel.Add(buffer.ReadPacketable<NetThing>());

            // Inventory
            int inventoryCount = buffer.ReadInt();
            Inventory = new List<NetThing>(inventoryCount);
            for (int i = 0; i < inventoryCount; i++)
                Inventory.Add(buffer.ReadPacketable<NetThing>());

            // Hediffs
            int hediffCount = buffer.ReadInt();
            Hediffs = new List<NetHediff>(hediffCount);
            for (int i = 0; i < hediffCount; i++)
                Hediffs.Add(buffer.ReadPacketable<NetHediff>());

            HealthState = (PawnHealthState) buffer.ReadByte();

            // Work priorities
            int priorityCount = buffer.ReadInt();
            WorkPriorities = new Dictionary<string, int>(priorityCount);
            for (int i = 0; i < priorityCount; i++)
                WorkPriorities.Add(buffer.ReadString(), buffer.ReadInt());

            // Records
            int recordCount = buffer.ReadInt();
            Records = new Dictionary<string, float>(recordCount);
            for (int i = 0; i < recordCount; i++)
                Records.Add(buffer.ReadString(), buffer.ReadFloat());

            // Inspiration
            InspirationDefName = buffer.ReadString(true);
            InspirationAge = buffer.ReadInt();
            InspirationReason = buffer.ReadString(true);

            // Player settings
            MedicalCare = (MedicalCareCategory) buffer.ReadByte();
            HostilityResponseMode = (RimWorld.HostilityResponseMode) buffer.ReadByte();
            SelfTend = buffer.ReadBoolean();

            // Schedule
            Schedule = new List<string>(24);
            for (int i = 0; i < 24; i++)
                Schedule.Add(buffer.ReadString());

            // Needs
            int needsCount = buffer.ReadInt();
            Needs = new List<NetNeed>(needsCount);
            for (int i = 0; i < needsCount; i++)
                Needs.Add(buffer.ReadPacketable<NetNeed>());

            // Royalty
            if (buffer.ReadBoolean())
                Royalty = buffer.ReadPacketable<NetRoyalty>();
        }

        public class NetSkill : IPacketable
        {
            public string SkillDefName;
            public int Level;
            public float Xp;
            public float XpSinceMidnight;
            public Passion Passion;
            
            public SkillDef SkillDef
            {
                get => DefDatabase<SkillDef>.GetNamed(SkillDefName);
                set => SkillDefName = value.defName;
            }

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(SkillDefName);
                buffer.WriteInt(Level);
                buffer.WriteDouble(Xp);
                buffer.WriteDouble(XpSinceMidnight);
                buffer.WriteByte((byte) Passion);
            }

            public void Read(PacketBuffer buffer)
            {
                SkillDefName = buffer.ReadString();
                Level = buffer.ReadInt();
                Xp = (float) buffer.ReadDouble();
                XpSinceMidnight = (float) buffer.ReadDouble();
                Passion = (Passion) buffer.ReadByte();
            }
        }

        public class NetTrait : IPacketable
        {
            public string TraitDefName;
            public int Degree;

            public TraitDef TraitDef
            {
                get => RimWorld.TraitDef.Named(TraitDefName);
                set => TraitDefName = value.defName;
            }

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(TraitDefName);
                buffer.WriteInt(Degree);
            }

            public void Read(PacketBuffer buffer)
            {
                TraitDefName = buffer.ReadString();
                Degree = buffer.ReadInt();
            }
        }

        public class NetNeed : IPacketable
        {
            public string NeedDefName;
            public float Level;

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(NeedDefName);
                buffer.WriteFloat(Level);
            }

            public void Read(PacketBuffer buffer)
            {
                NeedDefName = buffer.ReadString();
                Level = buffer.ReadFloat();
            }
        }
    }
}
