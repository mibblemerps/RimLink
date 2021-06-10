using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.Chat
{
    [Packet]
    public class PacketSendChatMessage : Packet
    {
        public string Message;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Message);
        }

        public override void Read(PacketBuffer buffer)
        {
            Message = buffer.ReadString();
        }
    }
}
