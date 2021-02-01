using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace PlayerTrade.Mechanoids.Designer
{
    public class MechCluster : IPacketable
    {
        public List<MechPartConfig> Parts = new List<MechPartConfig>();

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(Parts.Count);
            foreach (var part in Parts)
            {
                buffer.WriteString(part.GetType().FullName);
                buffer.WritePacketable(part);
            }
        }

        public void Read(PacketBuffer buffer)
        {
            int partCount = buffer.ReadInt();
            Parts = new List<MechPartConfig>(partCount);
            for (int i = 0; i < partCount; i++)
            {
                Type type = Type.GetType(buffer.ReadString());
                Log.Message("Reading part: " + type.FullName);
                MechPartConfig part = (MechPartConfig) Activator.CreateInstance(type);
                part.Read(buffer);
                Parts.Add(part);
            }
        }
    }
}
