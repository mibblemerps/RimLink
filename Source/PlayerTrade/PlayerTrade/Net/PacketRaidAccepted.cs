namespace PlayerTrade.Net
{
    public class PacketRaidAccepted : PacketForPlayer
    {
        public string Id;

        public override bool ShouldQueue => false;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Id);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Id = buffer.ReadString();
        }
    }
}
