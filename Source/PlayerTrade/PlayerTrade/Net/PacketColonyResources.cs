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
        public string Username;

        public Resources Resources;

        public PacketColonyResources()
        {
        }

        public PacketColonyResources(string username, Resources resources)
        {
            Username = username;
            Resources = resources;
        }

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Username);
            Resources.Write(buffer);
        }

        public override void Read(PacketBuffer buffer)
        {
            Username = buffer.ReadString();

            Resources = new Resources();
            Resources.Read(buffer);
        }
    }
}
