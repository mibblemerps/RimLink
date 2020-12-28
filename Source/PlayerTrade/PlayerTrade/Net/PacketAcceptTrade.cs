using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    /// <summary>
    /// Packet sent Client -> Server -> Client to accept a trade.
    /// </summary>
    public class PacketAcceptTrade : Packet
    {
        public Guid Trade;
        public bool Accept;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteGuid(Trade);
            buffer.WriteBoolean(Accept);
        }

        public override void Read(PacketBuffer buffer)
        {
            Trade = buffer.ReadGuid();
            Accept = buffer.ReadBoolean();
        }
    }
}
