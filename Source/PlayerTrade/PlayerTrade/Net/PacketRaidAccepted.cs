using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketRaidAccepted : Packet
    {
        public string For;
        public string Id;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(For);
            buffer.WriteString(Id);
        }

        public override void Read(PacketBuffer buffer)
        {
            For = buffer.ReadString();
            Id = buffer.ReadString();
        }
    }
}
