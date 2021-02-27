using System;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Trade.Packets
{
    /// <summary>
    /// Sent to confirm an accepted trade. This ensures the other party received and acknowledged the trade acception.
    /// </summary>
    [Packet]
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
