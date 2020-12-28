using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    /// <summary>
    /// Interface for reading/writing an object to the packet buffer, so it can be sent/received over the network.
    /// </summary>
    public interface IPacketable
    {
        void Write(PacketBuffer buffer);
        void Read(PacketBuffer buffer);
    }
}
