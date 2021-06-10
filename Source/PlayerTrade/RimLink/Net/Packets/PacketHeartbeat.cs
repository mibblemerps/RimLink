namespace RimLink.Net.Packets
{
    /// <summary>
    /// Packet sent periodically to ensure the connection is still alive.
    /// </summary>
    [Packet(Id = HeartbeatId, HideFromLog = true)]
    public class PacketHeartbeat : Packet
    {
        public override void Write(PacketBuffer buffer) {}

        public override void Read(PacketBuffer buffer) {}
    }
}
