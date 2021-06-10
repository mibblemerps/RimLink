namespace RimLink.Net.Packets
{
    /// <summary>
    /// Packet sent from the client -> server indicating they're disconnecting immediately. Upon sending or receiving this packet, the associated connection should be closed immediately.
    /// </summary>
    [Packet(Id = DisconnectId)]
    public class PacketDisconnect : Packet
    {
        public override void Write(PacketBuffer buffer) { }

        public override void Read(PacketBuffer buffer) { }
    }
}
