namespace PlayerTrade.Net.Packets
{
    [Packet(HideFromLog = true)]
    public class PacketColonyInfo : Packet
    {
        public string Guid;
        public Player Player;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            buffer.WritePacketable(Player);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
            Player = buffer.ReadPacketable<Player>();
        }
    }
}
