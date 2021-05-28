using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net.Packets;
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
        public List<NetMemory> Memories;

        public NetRoyalty Royalty;

        public NetHuman()
        {
            
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteMarker("HumanStart");

            buffer.WriteString(RimLinkGuid);

            // Name
            buffer.WriteMarker("Name");
            buffer.WriteInt(Name.Length);
            foreach (string name in Name)
                buffer.WriteString(name);

            buffer.WriteLong(ChronologicalAgeTicks);
            buffer.WriteLong(BiologicalAgeTicks);
            buffer.WriteString(KindDefName);
            buffer.WriteDouble(Melanin);
            buffer.WriteMarker("SkinColor");
            buffer.Write(SkinColor.ToFloats());
            buffer.WriteString(BodyTypeDefName);
            buffer.WriteByte((byte) CrownType);
            buffer.WriteMarker("HairColor");
            buffer.Write(HairColor.ToFloats());
            buffer.WriteString(HairDefName);
            buffer.WriteString(Childhood, true);
            buffer.WriteString(Adulthood, true);
            buffer.WriteByte((byte) Gender);

            // Skills
            buffer.WriteMarker("Skills");
            buffer.WriteInt(Skills.Count);
            foreach (NetSkill skill in Skills)
                buffer.WritePacketable(skill);

            // Traits
            buffer.WriteMarker("Traits");
            buffer.WriteInt(Traits.Count);
            foreach (NetTrait trait in Traits)
                buffer.WritePacketable(trait);

            // Equipment
            buffer.WriteMarker("Equipment");
            buffer.WriteInt(Equipment.Count);
            foreach (NetThing thing in Equipment)
                buffer.WritePacketable(thing);

            // Apparel
            buffer.WriteMarker("Apparel");
            buffer.WriteInt(Apparel.Count);
            foreach (NetThing thing in Apparel)
                buffer.WritePacketable(thing);

            // Inventory
            buffer.WriteMarker("Inventory");
            buffer.WriteInt(Inventory.Count);
            foreach (NetThing thing in Inventory)
                buffer.WritePacketable(thing);

            // Hediffs
            buffer.WriteMarker("Hediffs");
            buffer.WriteInt(Hediffs.Count);
            foreach (NetHediff hediff in Hediffs)
                buffer.WritePacketable(hediff);

            buffer.WriteByte((byte) HealthState);

            // Work priorities
            buffer.WriteMarker("WorkPriorities");
            buffer.WriteInt(WorkPriorities.Count);
            foreach (var priority in WorkPriorities)
            {
                buffer.WriteString(priority.Key);
                buffer.WriteInt(priority.Value);
            }

            // Records
            buffer.WriteMarker("Records");
            buffer.WriteInt(Records.Count);
            foreach (var record in Records)
            {
                buffer.WriteString(record.Key);
                buffer.WriteFloat(record.Value);
            }

            // Inspiration
            buffer.WriteMarker("Inspiration");
            buffer.WriteString(InspirationDefName, true);
            buffer.WriteInt(InspirationAge);
            buffer.WriteString(InspirationReason, true);

            // Player settings
            buffer.WriteMarker("PlayerSettings");
            buffer.WriteByte((byte) MedicalCare);
            buffer.WriteByte((byte) HostilityResponseMode);
            buffer.WriteBoolean(SelfTend);

            // Schedule
            buffer.WriteMarker("Schedule");
            for (int i = 0; i < 24; i++)
                buffer.WriteString(Schedule[i]);

            // Needs
            buffer.WriteMarker("Needs");
            buffer.WriteInt(Needs.Count);
            foreach (NetNeed need in Needs)
                buffer.WritePacketable(need);

            // Memories
            buffer.WriteMarker("Memories");
            buffer.WriteInt(Memories.Count);
            foreach (NetMemory memory in Memories)
                buffer.WritePacketable(memory);
            
            // Royalty
            buffer.WriteMarker("Royalty");
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
            buffer.ReadMarker("HumanStart");

            RimLinkGuid = buffer.ReadString();

            // Name
            buffer.ReadMarker("Name");
            int nameCount = buffer.ReadInt();
            Name = new string[nameCount];
            for (int i = 0; i < nameCount; i++)
                Name[i] = buffer.ReadString();

            ChronologicalAgeTicks = buffer.ReadLong();
            BiologicalAgeTicks = buffer.ReadLong();
            KindDefName = buffer.ReadString();
            Melanin = (float) buffer.ReadDouble();
            buffer.ReadMarker("SkinColor");
            SkinColor = buffer.Read<float[]>().ToColor();
            BodyTypeDefName = buffer.ReadString();
            CrownType = (CrownType) buffer.ReadByte();
            buffer.ReadMarker("HairColor");
            HairColor = buffer.Read<float[]>().ToColor();
            HairDefName = buffer.ReadString();
            Childhood = buffer.ReadString(true);
            Adulthood = buffer.ReadString(true);
            Gender = (Gender) buffer.ReadByte();

            // Skills
            buffer.ReadMarker("Skills");
            int skillCount = buffer.ReadInt();
            Skills = new List<NetSkill>(skillCount);
            for (int i = 0; i < skillCount; i++)
                Skills.Add(buffer.ReadPacketable<NetSkill>());

            // Traits
            buffer.ReadMarker("Traits");
            int traitsCount = buffer.ReadInt();
            Traits = new List<NetTrait>(traitsCount);
            for (int i = 0; i < traitsCount; i++)
                Traits.Add(buffer.ReadPacketable<NetTrait>());

            // Equipment
            buffer.ReadMarker("Equipment");
            int equipmentCount = buffer.ReadInt();
            Equipment = new List<NetThing>(equipmentCount);
            for (int i = 0; i < equipmentCount; i++)
                Equipment.Add(buffer.ReadPacketable<NetThing>());

            // Apparel
            buffer.ReadMarker("Apparel");
            int apparelCount = buffer.ReadInt();
            Apparel = new List<NetThing>(apparelCount);
            for (int i = 0; i < apparelCount; i++)
                Apparel.Add(buffer.ReadPacketable<NetThing>());

            // Inventory
            buffer.ReadMarker("Inventory");
            int inventoryCount = buffer.ReadInt();
            Inventory = new List<NetThing>(inventoryCount);
            for (int i = 0; i < inventoryCount; i++)
                Inventory.Add(buffer.ReadPacketable<NetThing>());

            // Hediffs
            buffer.ReadMarker("Hediffs");
            int hediffCount = buffer.ReadInt();
            Hediffs = new List<NetHediff>(hediffCount);
            for (int i = 0; i < hediffCount; i++)
                Hediffs.Add(buffer.ReadPacketable<NetHediff>());

            HealthState = (PawnHealthState) buffer.ReadByte();

            // Work priorities
            buffer.ReadMarker("WorkPriorities");
            int priorityCount = buffer.ReadInt();
            WorkPriorities = new Dictionary<string, int>(priorityCount);
            for (int i = 0; i < priorityCount; i++)
                WorkPriorities.SetOrAdd(buffer.ReadString(), buffer.ReadInt());

            // Records
            buffer.ReadMarker("Records");
            int recordCount = buffer.ReadInt();
            Records = new Dictionary<string, float>(recordCount);
            for (int i = 0; i < recordCount; i++)
                Records.SetOrAdd(buffer.ReadString(), buffer.ReadFloat());

            // Inspiration
            buffer.ReadMarker("Inspiration");
            InspirationDefName = buffer.ReadString(true);
            InspirationAge = buffer.ReadInt();
            InspirationReason = buffer.ReadString(true);

            // Player settings
            buffer.ReadMarker("PlayerSettings");
            MedicalCare = (MedicalCareCategory) buffer.ReadByte();
            HostilityResponseMode = (RimWorld.HostilityResponseMode) buffer.ReadByte();
            SelfTend = buffer.ReadBoolean();

            // Schedule
            buffer.ReadMarker("Schedule");
            Schedule = new List<string>(24);
            for (int i = 0; i < 24; i++)
                Schedule.Add(buffer.ReadString());

            // Needs
            buffer.ReadMarker("Needs");
            int needsCount = buffer.ReadInt();
            Needs = new List<NetNeed>(needsCount);
            for (int i = 0; i < needsCount; i++)
                Needs.Add(buffer.ReadPacketable<NetNeed>());

            // Memories
            buffer.ReadMarker("Memories");
            int memoriesCount = buffer.ReadInt();
            Memories = new List<NetMemory>(memoriesCount);
            for (int i = 0; i < memoriesCount; i++)
                Memories.Add(buffer.ReadPacketable<NetMemory>());
            
            // Royalty
            buffer.ReadMarker("Royalty");
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

        public class NetMemory : IPacketable
        {
            public string ThoughtDefName;
            public float MoodPowerFactor;
            public int Age;
            public int Stage;
            
            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(ThoughtDefName);
                buffer.WriteFloat(MoodPowerFactor);
                buffer.WriteInt(Age);
                buffer.WriteInt(Stage);
            }

            public void Read(PacketBuffer buffer)
            {
                ThoughtDefName = buffer.ReadString();
                MoodPowerFactor = buffer.ReadFloat();
                Age = buffer.ReadInt();
                Stage = buffer.ReadInt();
            }
        }
    }
}
