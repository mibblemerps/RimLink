namespace RimLink.Net.Packets
{
    [Packet]
    public class PacketAcknowledgement : Packet
    {
        public string Guid;
        public bool Success;
        public string FailReason;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            buffer.WriteBoolean(Success);
            buffer.WriteString(FailReason, true);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
            Success = buffer.ReadBoolean();
            FailReason = buffer.ReadString(true);
        }
    }
}
