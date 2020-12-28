using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketConnect : Packet
    {
        public int ProtocolVersion;
        public string Username;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(ProtocolVersion);
            buffer.WriteString(Username);
        }

        public override void Read(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt();
            Username = buffer.ReadString();
        }
    }
}
