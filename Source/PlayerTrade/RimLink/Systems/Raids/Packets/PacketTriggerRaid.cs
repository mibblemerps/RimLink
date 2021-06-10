using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Raids.Packets
{
    [Packet]
    public class PacketTriggerRaid : PacketForPlayer
    {
        public BountyRaid Raid;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.Write(Raid);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Raid = buffer.Read<BountyRaid>();
        }
    }
}
