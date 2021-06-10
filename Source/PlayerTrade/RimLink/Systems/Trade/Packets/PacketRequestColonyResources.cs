using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Trade.Packets
{
    /// <summary>
    /// Packet sent Server -> Client asking the client to send their current colony resources.
    /// </summary>
    [Packet]
    public class PacketRequestColonyResources : Packet
    {
        public override void Write(PacketBuffer buffer) {}

        public override void Read(PacketBuffer buffer) {}
    }
}
