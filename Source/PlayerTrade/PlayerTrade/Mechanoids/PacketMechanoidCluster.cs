using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Mechanoids.Designer;
using PlayerTrade.Net;

namespace PlayerTrade.Mechanoids
{
    public class PacketMechanoidCluster : PacketForPlayer
    {
        public string From;
        public MechCluster Cluster;

        public override bool ShouldQueue => true;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(From);
            buffer.WritePacketable(Cluster);
        }

        public override void Read(PacketBuffer buffer)
        {
            From = buffer.ReadString();
            Cluster = buffer.ReadPacketable<MechCluster>();
        }
    }
}
