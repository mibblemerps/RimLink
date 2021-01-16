using System;

namespace PlayerTrade.Net
{
    public class PacketAnnouncement : Packet
    {
        public string Message;
        public MessageType Type = MessageType.Dialog;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Message);
            buffer.WriteByte((byte) Type);
        }

        public override void Read(PacketBuffer buffer)
        {
            Message = buffer.ReadString();
            Type = (MessageType) buffer.ReadByte();
        }

        public enum MessageType
        {
            Dialog,
            Message
        }
    }
}
