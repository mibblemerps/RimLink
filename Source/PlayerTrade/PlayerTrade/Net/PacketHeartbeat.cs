namespace PlayerTrade.Net
{
    /// <summary>
    /// Packet sent periodically to ensure the connection is still alive.
    /// </summary>
    public class PacketHeartbeat : Packet
    {
        public override void Write(PacketBuffer buffer) {}

        public override void Read(PacketBuffer buffer) {}
    }
}
