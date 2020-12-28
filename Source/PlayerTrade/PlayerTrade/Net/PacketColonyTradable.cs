using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketColonyTradable : Packet
    {
        public string Username;
        public bool TradableNow;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Username);
            buffer.WriteBoolean(TradableNow);
        }

        public override void Read(PacketBuffer buffer)
        {
            Username = buffer.ReadString();
            TradableNow = buffer.ReadBoolean();
        }
    }
}
