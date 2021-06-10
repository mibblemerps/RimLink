namespace RimLink.Net.Packets
{
    /// <summary>
    /// Packet sent by server to inform client they're an admin (or not).
    /// </summary>
    [Packet]
    public class PacketAdmin : Packet
    {
        public bool IsAdmin;
        
        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteBoolean(IsAdmin);
        }

        public override void Read(PacketBuffer buffer)
        {
            IsAdmin = buffer.ReadBoolean();
        }
    }
}