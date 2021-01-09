using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketConfirmLaborOffer : PacketForPlayer
    {
        public string Guid;
        public bool Confirm;

        public override void Write(PacketBuffer buffer)
        {
            base.Write(buffer);
            buffer.WriteString(Guid);
            buffer.WriteBoolean(Confirm);
        }

        public override void Read(PacketBuffer buffer)
        {
            base.Read(buffer);
            Guid = buffer.ReadString();
            Confirm = buffer.ReadBoolean();
        }
    }
}
