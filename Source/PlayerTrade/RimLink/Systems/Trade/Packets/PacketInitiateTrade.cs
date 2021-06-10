using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Trade.Packets
{
    /// <summary>
    /// A packet send Client -> Server to initiate a trade with another player. The server responds with a packet containing that colonies resources.
    /// </summary>
    [Packet]
    public class PacketInitiateTrade : Packet
    {
        public string Guid;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
        }
    }
}
