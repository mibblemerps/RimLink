using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Systems.Mechanoids.Designer;

namespace RimLink.Systems.Mechanoids
{
    [Packet]
    public class PacketMechanoidCluster : PacketForPlayer
    {
        public string From;
        public MechCluster Cluster;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(From);
            buffer.WritePacketable(Cluster);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            From = buffer.ReadString();
            Cluster = buffer.ReadPacketable<MechCluster>();
        }
    }
}
