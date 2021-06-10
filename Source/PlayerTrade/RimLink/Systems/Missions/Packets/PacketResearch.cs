using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Missions.Packets
{
    /// <summary>
    /// Sent when a lent colonist is doing joint research. This adds research points onto the host colonies research.
    /// </summary>
    [Packet(HideFromLog = true)]
    public class PacketResearch : PacketForPlayer
    {
        public override bool ShouldQueue => true;

        public float Research;

        public override bool MergeWithExistingPacket(PacketForPlayer packet)
        {
            // Merge research points into previous queued packet
            ((PacketResearch) packet).Research += Research;
            return true;
        }

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteFloat(Research);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Research = buffer.ReadFloat();
        }
    }
}
