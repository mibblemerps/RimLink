using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    /// <summary>
    /// Packet sent Server -> Client asking the client to send their current colony resources.
    /// </summary>
    public class PacketRequestColonyResources : Packet
    {
        public override void Write(PacketBuffer buffer) {}

        public override void Read(PacketBuffer buffer) {}
    }
}
