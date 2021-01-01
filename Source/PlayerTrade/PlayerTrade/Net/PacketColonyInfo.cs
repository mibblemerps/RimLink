using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketColonyInfo : Packet
    {
        public string Guid;
        public Player Player;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            buffer.Write(Player);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
            Player = buffer.Read<Player>();
        }
    }
}
