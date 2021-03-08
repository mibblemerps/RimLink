using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Missions.Packets
{
    [Packet]
    public class PacketAcceptMissionOffer : PacketForPlayer
    {
        public string Guid;
        public bool Accept;

        public override bool ShouldQueue => false;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteBoolean(Accept);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadString();
            Accept = buffer.ReadBoolean();
        }
    }
}
