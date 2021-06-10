namespace RimLink.Net.Packets
{
    /// <summary>
    /// Packet sent from Server -> Client indicating it's being kicked. The connection will be closed immediately after.
    /// </summary>
    [Packet]
    public class PacketKick : Packet
    {
        public string Reason;
        public bool AllowReconnect;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Reason, true);
            buffer.WriteBoolean(AllowReconnect);
        }

        public override void Read(PacketBuffer buffer)
        {
            Reason = buffer.ReadString(true);
            AllowReconnect = buffer.ReadBoolean();
        }
    }
}
