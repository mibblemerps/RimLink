using RimLink.Core;
using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Trade.Packets
{
    /// <summary>
    /// Packet contains all the resources the colony has available to trade with other players.
    /// </summary>
    [Packet]
    public class PacketColonyResources : Packet
    {
        public string Guid;

        public Resources Resources;

        public PacketColonyResources(string guid, Resources resources)
        {
            Guid = guid;
            Resources = resources;
        }

        // Empty constructor needed for packet to be instantiated when received
        public PacketColonyResources() {}

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            buffer.WritePacketable(Resources);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
            Resources = buffer.ReadPacketable<Resources>();
        }
    }
}
