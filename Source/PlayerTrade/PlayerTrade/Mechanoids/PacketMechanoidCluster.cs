using PlayerTrade.Mechanoids.Designer;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Mechanoids
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
