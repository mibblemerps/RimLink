namespace RimLink.Net.Packets
{
    [Packet]
    public class PacketPlayerDisconnected : Packet
    {
        public string Player;
        public string Reason;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Player);
            buffer.WriteString(Reason, true);
        }

        public override void Read(PacketBuffer buffer)
        {
            Player = buffer.ReadString();
            Reason = buffer.ReadString(true);
        }
    }
}
