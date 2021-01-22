using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade
{
    /// <summary>
    /// Represents a colonies resources.
    /// </summary>
    public class Resources : IPacketable
    {
        public List<NetThing> Things = new List<NetThing>();
        public List<NetHuman> Pawns = new List<NetHuman>();

        public void Write(PacketBuffer buffer)
        {
            // Write things
            buffer.WriteInt(Things.Count);
            foreach (NetThing netThing in Things)
                netThing.Write(buffer);

            // Write pawns
            buffer.WriteInt(Pawns.Count);
            foreach (NetHuman netHuman in Pawns)
                buffer.WritePacketable(netHuman);
        }

        public void Read(PacketBuffer buffer)
        {
            Things.Clear();

            // Read things
            int thingsCount = buffer.ReadInt();
            Things = new List<NetThing>(thingsCount);
            for (int i = 0; i < thingsCount; i++)
                Things.Add(buffer.ReadPacketable<NetThing>());

            // Read pawns
            int pawnsCount = buffer.ReadInt();
            Pawns = new List<NetHuman>(pawnsCount);
            for (int i = 0; i < pawnsCount; i++)
                Pawns.Add(buffer.ReadPacketable<NetHuman>());
        }

        public void Update(Map map)
        {
            Things.Clear();

            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(map))
                Things.Add(NetThing.FromThing(thing));

            foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(map).Where(p => p.RaceProps.Humanlike))
                Pawns.Add(pawn.ToNetHuman());
        }
    }
}
