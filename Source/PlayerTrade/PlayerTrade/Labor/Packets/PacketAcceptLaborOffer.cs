using PlayerTrade.Net;

namespace PlayerTrade.Labor.Packets
{
    public class PacketAcceptLaborOffer : PacketForPlayer
    {
        public string Guid;
        public bool Accept;

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
