using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Raids;

namespace PlayerTrade.Net
{
    public class PacketTriggerRaid : Packet
    {
        public string For;
        public BountyRaid Raid;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(For);
            buffer.Write(Raid);
        }

        public override void Read(PacketBuffer buffer)
        {
            For = buffer.ReadString();
            Raid = buffer.Read<BountyRaid>();
        }
    }
}
