using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    /// <summary>
    /// Packet contains all the resources the colony has available to trade with other players.
    /// </summary>
    public class PacketColonyResources : Packet
    {
        public string Guid;

        public Resources Resources;

        public PacketColonyResources()
        {
        }

        public PacketColonyResources(string guid, Resources resources)
        {
            Guid = guid;
            Resources = resources;
        }

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            Resources.Write(buffer);
        }

        public override void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();

            Resources = new Resources();
            Resources.Read(buffer);
        }
    }
}
