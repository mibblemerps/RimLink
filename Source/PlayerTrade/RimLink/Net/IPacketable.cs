using RimLink.Net.Packets;

namespace RimLink.Net
{
    /// <summary>
    /// Interface for reading/writing an object to the packet buffer, so it can be sent/received over the network.
    /// </summary>
    public interface IPacketable
    {
        void Write(PacketBuffer buffer);
        void Read(PacketBuffer buffer);
    }
}
