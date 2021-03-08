using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Missions.Packets
{
    [Packet]
    public class PacketConfirmMissionOffer : PacketForPlayer
    {
        public string Guid;
        public bool Confirm;

        public override bool ShouldQueue => false;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteBoolean(Confirm);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadString();
            Confirm = buffer.ReadBoolean();
        }
    }
}
