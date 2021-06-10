using System.Collections.Generic;
using RimLink.Net.Packets;

namespace RimLink.Net
{
    public class NetRoyalty : IPacketable
    {
        public int Favor;
        public int PermitPoints;
        public List<NetPermit> Permits = new List<NetPermit>();
        public List<NetTitle> Titles = new List<NetTitle>();
        public int LastDecreeTicksAgo;
        public bool AllowRoomRequirements;
        public bool AllowApparelRequirements;

        public bool HasPsylink;
        public float CurrentEntropy;
        public float CurrentPsyfocus;
        public float TargetPsyfocus;
        public float LastMeditationTick;
        public bool LimitEntropyAmount;

        /// <summary>
        /// A basic dummy pawn heir
        /// </summary>
        public NetHuman DummyHeir;

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(Favor);
            buffer.WriteInt(PermitPoints);
            buffer.WriteList<NetPermit>(Permits, (b, item) => { b.WritePacketable(item); });
            buffer.WriteList<NetTitle>(Titles, (b, item) => { b.WritePacketable(item); });
            buffer.WriteInt(LastDecreeTicksAgo);
            buffer.WriteBoolean(AllowRoomRequirements);
            buffer.WriteBoolean(AllowApparelRequirements);
            
            buffer.WriteBoolean(HasPsylink);
            if (HasPsylink)
            {
                buffer.WriteFloat(CurrentEntropy);
                buffer.WriteFloat(CurrentPsyfocus);
                buffer.WriteFloat(TargetPsyfocus);
                buffer.WriteFloat(LastMeditationTick);
                buffer.WriteBoolean(LimitEntropyAmount);
            }

            buffer.WritePacketable(DummyHeir, true);
        }

        public void Read(PacketBuffer buffer)
        {
            Favor = buffer.ReadInt();
            PermitPoints = buffer.ReadInt();
            Permits = buffer.ReadList<NetPermit>(b => b.ReadPacketable<NetPermit>());
            Titles = buffer.ReadList<NetTitle>(b => b.ReadPacketable<NetTitle>());
            LastDecreeTicksAgo = buffer.ReadInt();
            AllowRoomRequirements = buffer.ReadBoolean();
            AllowApparelRequirements = buffer.ReadBoolean();

            HasPsylink = buffer.ReadBoolean();
            if (HasPsylink)
            {
                CurrentEntropy = buffer.ReadFloat();
                CurrentPsyfocus = buffer.ReadFloat();
                TargetPsyfocus = buffer.ReadFloat();
                LastMeditationTick = buffer.ReadFloat();
                LimitEntropyAmount = buffer.ReadBoolean();
            }

            DummyHeir = buffer.ReadPacketable<NetHuman>(true);
        }

        public class NetTitle : IPacketable
        {
            public string RoyalTitleDefName;
            public int GotTicksAgo;
            public bool WasInherited;
            public bool Conceited;

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(RoyalTitleDefName);
                buffer.WriteInt(GotTicksAgo);
                buffer.WriteBoolean(WasInherited);
                buffer.WriteBoolean(Conceited);
            }

            public void Read(PacketBuffer buffer)
            {
                RoyalTitleDefName = buffer.ReadString();
                GotTicksAgo = buffer.ReadInt();
                WasInherited = buffer.ReadBoolean();
                Conceited = buffer.ReadBoolean();
            }
        }

        public class NetPermit : IPacketable
        {
            public string PermitDefName;
            public string TitleDefName;
            public int UsedTicksAgo;

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(PermitDefName);
                buffer.WriteString(TitleDefName);
                buffer.WriteInt(UsedTicksAgo);
            }

            public void Read(PacketBuffer buffer)
            {
                PermitDefName = buffer.ReadString();
                TitleDefName = buffer.ReadString();
                UsedTicksAgo = buffer.ReadInt();
            }
        }
    }
}
