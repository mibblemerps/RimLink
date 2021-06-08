namespace PlayerTrade.Net.Packets
{
    /// <summary>
    /// Sent from server -> client to enable dev mode.
    /// </summary>
    [Packet]
    public class PacketDevMode : Packet
    {
        public bool Enable;
        
        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteBoolean(Enable);
        }

        public override void Read(PacketBuffer buffer)
        {
            Enable = buffer.ReadBoolean();
        }
    }
}