using PlayerTrade.Net;

namespace PlayerTrade.Labor.Packets
{
    public class PacketColonistLost : PacketForPlayer
    {
        public override bool ShouldQueue => true;

        public string PawnGuid;
        public LostType How;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(PawnGuid);
            buffer.WriteByte((byte) How);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            PawnGuid = buffer.ReadString();
            How = (LostType) buffer.ReadByte();
        }

        public enum LostType
        {
            Gone,
            Dead,
            Imprisoned
        }
    }
}
