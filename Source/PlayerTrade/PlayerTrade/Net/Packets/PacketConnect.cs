namespace PlayerTrade.Net.Packets
{
    [Packet(Id = ConnectId)]
    public class PacketConnect : Packet
    {
        public int ProtocolVersion;
        public string Guid;
        public string Secret;
        public Player Player;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(ProtocolVersion);
            buffer.WriteString(Guid);
            buffer.WriteString(Secret);
            buffer.WritePacketable(Player);
        }

        public override void Read(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt();
            Guid = buffer.ReadString();
            Secret = buffer.ReadString();
            Player = buffer.ReadPacketable<Player>();
        }
    }
}
