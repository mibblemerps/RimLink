namespace PlayerTrade.Net.Packets
{
    [Packet]
    public class PacketPing : Packet
    {
        public int ProtocolVersion;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(ProtocolVersion);
        }

        public override void Read(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt();
        }
    }
}
