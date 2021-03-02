using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Labor.Packets
{
    [Packet]
    public class PacketLentColonistUpdate : PacketForPlayer
    {
        public override bool ShouldQueue => true;

        public string PawnGuid;
        public ColonistEvent What;
        public string EscapeDefName;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(PawnGuid);
            buffer.WriteByte((byte) What);
            buffer.WriteString(EscapeDefName, true);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            PawnGuid = buffer.ReadString();
            What = (ColonistEvent) buffer.ReadByte();
            EscapeDefName = buffer.ReadString(true);
        }

        public enum ColonistEvent
        {
            Gone,
            Dead,
            Imprisoned,
            Escaped
        }
    }
}
