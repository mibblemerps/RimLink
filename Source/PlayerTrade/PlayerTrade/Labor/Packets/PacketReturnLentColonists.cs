using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Labor.Packets
{
    [Packet]
    public class PacketReturnLentColonists : PacketForPlayer
    {
        public string Guid;
        public List<NetHuman> ReturnedColonists;
        public bool Escaped;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteInt(ReturnedColonists.Count);
            foreach (var colonist in ReturnedColonists)
                buffer.WritePacketable(colonist);
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
            Escaped = buffer.ReadBoolean();
        }
    }
}
