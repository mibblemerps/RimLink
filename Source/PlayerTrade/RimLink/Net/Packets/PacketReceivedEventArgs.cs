using System;

namespace RimLink.Net.Packets
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
