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

        /// <summary>
        /// Should this packet be saved and queued up if the target player is offline, and be delivered to them next time they connect?
        /// </summary>
        public abstract bool ShouldQueue { get; }

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
