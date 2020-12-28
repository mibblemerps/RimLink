using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public readonly int Id;
        public readonly Packet Packet;

        public PacketReceivedEventArgs(int id, Packet packet)
        {
            Id = id;
            Packet = packet;
        }
    }
}
