using System.Collections.Generic;
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
            
            buffer.WriteMarker("Groups");

            // Skills
            buffer.WriteGroup("Skills", group =>
            {
                group.WriteInt(Skills.Count);
                foreach (NetSkill skill in Skills)
                    group.WritePacketable(skill);
            });
            
            // Traits
            buffer.WriteGroup("Traits", group =>
            {
                group.WriteInt(Traits.Count);
                foreach (NetTrait trait in Traits)
                    group.WritePacketable(trait);
            });

            // Equipment
            buffer.WriteGroup("Equipment", group =>
            {
                group.WriteInt(Equipment.Count);
                foreach (NetThing thing in Equipment)
                    group.WritePacketable(thing);                
            });

            // Apparel
            buffer.WriteGroup("Apparel", group =>
            {
                group.WriteInt(Apparel.Count);
                foreach (NetThing thing in Apparel)
                    group.WritePacketable(thing);                
            });

            // Inventory
            buffer.WriteGroup("Inventory", group =>
            {
                group.WriteInt(Inventory.Count);
                foreach (NetThing thing in Inventory)
                    group.WritePacketable(thing);                
            });

            // Hediffs
            buffer.WriteGroup("Hediffs", group =>
            {
                group.WriteInt(Hediffs.Count);
                foreach (NetHediff hediff in Hediffs)
                    group.WritePacketable(hediff);
                
                group.WriteByte((byte) HealthState);
            });

            // Work priorities
            buffer.WriteGroup("WorkPriorities", group =>
            {
                group.WriteInt(WorkPriorities.Count);
                foreach (var priority in WorkPriorities)
                {
                    group.WriteString(priority.Key);
                    group.WriteInt(priority.Value);
                }                
            });

            // Records
            buffer.WriteGroup("Records", group =>
            {
                group.WriteInt(Records.Count);
                foreach (var record in Records)
                {
                    group.WriteString(record.Key);
                    group.WriteFloat(record.Value);
                }
            });

            // Inspiration
            buffer.WriteGroup("Inspiration", group =>
            {
                group.WriteString(InspirationDefName, true);
                group.WriteInt(InspirationAge);
                group.WriteString(InspirationReason, true);                
            });

            // Player settings
            buffer.WriteGroup("PlayerSettings", group =>
            {
                group.WriteByte((byte) MedicalCare);
                group.WriteByte((byte) HostilityResponseMode);
                group.WriteBoolean(SelfTend);                
            });

            // Schedule
            buffer.WriteGroup("Schedule", group =>
            {
                for (int i = 0; i < 24; i++)
                    group.WriteString(Schedule[i]);                
            });

            // Needs
            buffer.WriteGroup("Needs", group =>
            {
                group.WriteInt(Needs.Count);
                foreach (NetNeed need in Needs)
                    group.WritePacketable(need);                
            });

            // Memories
            buffer.WriteGroup("Memories", group =>
            {
                group.WriteInt(Memories.Count);
                foreach (NetMemory memory in Memories)
                    group.WritePacketable(memory);                
            });

            // Royalty
            buffer.WriteGroup("Royalty", group =>
            {
                if (Royalty == null)
                {
                    group.WriteBoolean(false);
                }
                else
                {
                    group.WriteBoolean(true);
                    group.WritePacketable(Royalty);
                }                
            });
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
            
            buffer.ReadMarker("Groups");

            // Skills
            buffer.ReadGroup("Skills", group =>
            {
                int skillCount = group.ReadInt();
                Skills = new List<NetSkill>(skillCount);
                for (int i = 0; i < skillCount; i++)
                    Skills.Add(group.ReadPacketable<NetSkill>());                
            }, () => Skills = null);

            // Traits
            buffer.ReadGroup("Traits", group =>
            {
                int traitsCount = group.ReadInt();
                Traits = new List<NetTrait>(traitsCount);
                for (int i = 0; i < traitsCount; i++)
                    Traits.Add(group.ReadPacketable<NetTrait>());                
            }, () => Traits = null);

            // Equipment
            buffer.ReadGroup("Equipment", group =>
            {
                int equipmentCount = group.ReadInt();
                Equipment = new List<NetThing>(equipmentCount);
                for (int i = 0; i < equipmentCount; i++)
                    Equipment.Add(group.ReadPacketable<NetThing>());                
            }, () => Equipment = null);

            // Apparel
            buffer.ReadGroup("Apparel", group =>
            {
                int apparelCount = group.ReadInt();
                Apparel = new List<NetThing>(apparelCount);
                for (int i = 0; i < apparelCount; i++)
                    Apparel.Add(group.ReadPacketable<NetThing>());                
            }, () => Apparel = null);

            // Inventory
            buffer.ReadGroup("Inventory", group =>
            {
                int inventoryCount = group.ReadInt();
                Inventory = new List<NetThing>(inventoryCount);
                for (int i = 0; i < inventoryCount; i++)
                    Inventory.Add(group.ReadPacketable<NetThing>());                
            }, () => Inventory = null);

            // Hediffs
            buffer.ReadGroup("Hediffs", group =>
            {
                int hediffCount = group.ReadInt();
                Hediffs = new List<NetHediff>(hediffCount);
                for (int i = 0; i < hediffCount; i++)
                    Hediffs.Add(group.ReadPacketable<NetHediff>());
                
                HealthState = (PawnHealthState) group.ReadByte();
            }, () => Hediffs = null);

            // Work priorities
            buffer.ReadGroup("WorkPriorities", group =>
            {
                int priorityCount = group.ReadInt();
                WorkPriorities = new Dictionary<string, int>(priorityCount);
                for (int i = 0; i < priorityCount; i++)
                    WorkPriorities.SetOrAdd(group.ReadString(), group.ReadInt());                
            }, () => WorkPriorities = null);

            // Records
            buffer.ReadGroup("Records", group =>
            {
                int recordCount = group.ReadInt();
                Records = new Dictionary<string, float>(recordCount);
                for (int i = 0; i < recordCount; i++)
                    Records.SetOrAdd(group.ReadString(), group.ReadFloat());                
            }, () => Records = null);

            // Inspiration
            buffer.ReadGroup("Inspiration", group =>
            {
                InspirationDefName = group.ReadString(true);
                InspirationAge = group.ReadInt();
                InspirationReason = group.ReadString(true);                
            });

            // Player settings
            buffer.ReadGroup("PlayerSettings", group =>
            {
                MedicalCare = (MedicalCareCategory) group.ReadByte();
                HostilityResponseMode = (HostilityResponseMode) group.ReadByte();
                SelfTend = group.ReadBoolean();                
            });

            // Schedule
            buffer.ReadGroup("Schedule", group =>
            {
                Schedule = new List<string>(24);
                for (int i = 0; i < 24; i++)
                    Schedule.Add(group.ReadString());                
            }, () => Schedule = null);

            // Needs
            buffer.ReadGroup("Needs", group =>
            {
                int needsCount = group.ReadInt();
                Needs = new List<NetNeed>(needsCount);
                for (int i = 0; i < needsCount; i++)
                    Needs.Add(group.ReadPacketable<NetNeed>());                
            }, () => Needs = null);

            // Memories
            buffer.ReadGroup("Memories", group =>
            {
                int memoriesCount = group.ReadInt();
                Memories = new List<NetMemory>(memoriesCount);
                for (int i = 0; i < memoriesCount; i++)
                    Memories.Add(group.ReadPacketable<NetMemory>());                
            }, () => Memories = null);

            // Royalty
            buffer.ReadGroup("Royalty", group =>
            {
                if (group.ReadBoolean())
                    Royalty = group.ReadPacketable<NetRoyalty>();                
            }, () => Royalty = null);
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
                get => TraitDef.Named(TraitDefName);
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
