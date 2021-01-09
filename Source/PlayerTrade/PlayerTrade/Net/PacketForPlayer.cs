using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public abstract class PacketForPlayer : Packet
    {
        public string For;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(For);
        }

        public override void Read(PacketBuffer buffer)
        {
            For = buffer.ReadString();
        }
    }
}
