namespace PlayerTrade.Net
{
    /// <summary>
    /// A player reported bug report.
    /// </summary>
    public class PacketBugReport : Packet
    {
        public string Description;
        public string Log;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Description, true);
            buffer.WriteString(Log, true);
        }

        public override void Read(PacketBuffer buffer)
        {
            Description = buffer.ReadString(true);
            Log = buffer.ReadString(true);
        }
    }
}
