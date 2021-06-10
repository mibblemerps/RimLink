using System.Collections.Generic;
using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Missions.Packets
{
    [Packet]
    public class PacketReturnLentColonists : PacketForPlayer
    {
        public string Guid;
        public List<NetHuman> ReturnedColonists;
        public bool MainGroup;
        public bool Escaped;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteInt(ReturnedColonists.Count);
            foreach (var colonist in ReturnedColonists)
                buffer.WritePacketable(colonist);
            buffer.WriteBoolean(MainGroup);
            buffer.WriteBoolean(Escaped);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadString();
            int returnedColonistCount = buffer.ReadInt();
            ReturnedColonists = new List<NetHuman>(returnedColonistCount);
            for (int i = 0; i < returnedColonistCount; i++)
                ReturnedColonists.Add(buffer.ReadPacketable<NetHuman>());
            MainGroup = buffer.ReadBoolean();
            Escaped = buffer.ReadBoolean();
        }
    }
}
