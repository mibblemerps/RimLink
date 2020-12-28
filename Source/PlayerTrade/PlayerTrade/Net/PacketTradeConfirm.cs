using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    /// <summary>
    /// Sent to confirm an accepted trade. This ensures the other party received and acknowledged the trade acception.
    /// </summary>
    public class PacketTradeConfirm : Packet
    {
        public Guid Trade;
        /// <summary>
        /// Whether the trade should go ahead.
        /// </summary>
        public bool Confirm;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteGuid(Trade);
            buffer.WriteBoolean(Confirm);
        }

        public override void Read(PacketBuffer buffer)
        {
            Trade = buffer.ReadGuid();
            Confirm = buffer.ReadBoolean();
        }
    }
}
