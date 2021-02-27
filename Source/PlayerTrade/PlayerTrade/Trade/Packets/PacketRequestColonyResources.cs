using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Trade.Packets
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
