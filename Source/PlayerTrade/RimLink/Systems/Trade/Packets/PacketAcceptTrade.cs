using System;
using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Trade.Packets
{
    /// <summary>
    /// Packet sent Client -> Server -> Client to accept a trade.
    /// </summary>
    [Packet]
    public class PacketAcceptTrade : Packet
    {
        public Guid Trade;
        public bool Accept;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteGuid(Trade);
            buffer.WriteBoolean(Accept);
        }

        public override void Read(PacketBuffer buffer)
        {
            Trade = buffer.ReadGuid();
            Accept = buffer.ReadBoolean();
        }
    }
}
