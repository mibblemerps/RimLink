using System;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Trade.Packets
{
    [Packet]
    public class PacketRetractTrade : PacketForPlayer
    {
        public Guid Guid;
        
        public override bool ShouldQueue => false;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteGuid(Guid);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadGuid();
        }
    }
}