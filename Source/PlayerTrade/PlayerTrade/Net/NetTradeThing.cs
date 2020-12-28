using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PlayerTrade.Net
{
    [Serializable]
    public class NetTradeThing : IPacketable
    {
        public List<NetThing> OfferedThings = new List<NetThing>();
        public List<NetThing> RequestedThings = new List<NetThing>();
        public int CountOffered;

        public TradeOffer.TradeThing ToTradeThing()
        {
            var offered = new List<Thing>();
            var requested = new List<Thing>();
            foreach (NetThing netThing in OfferedThings)
                offered.Add(netThing.ToThing());
            foreach (NetThing netThing in RequestedThings)
                requested.Add(netThing.ToThing());

            return new TradeOffer.TradeThing(offered, requested, CountOffered);
        }

        public static NetTradeThing FromTradeThing(TradeOffer.TradeThing tradeThing)
        {
            var net = new NetTradeThing();
            net.CountOffered = tradeThing.CountOffered;
            foreach (Thing thing in tradeThing.RequestedThings)
                net.RequestedThings.Add(NetThing.FromThing(thing));
            foreach (Thing thing in tradeThing.OfferedThings)
                net.OfferedThings.Add(NetThing.FromThing(thing));
            return net;
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(OfferedThings.Count);
            foreach (NetThing thing in OfferedThings)
                thing.Write(buffer);

            buffer.WriteInt(RequestedThings.Count);
            foreach (NetThing thing in RequestedThings)
                thing.Write(buffer);

            buffer.WriteInt(CountOffered);
        }

        public void Read(PacketBuffer buffer)
        {
            int offeredCount = buffer.ReadInt();
            for (int i = 0; i < offeredCount; i++)
            {
                var thing = new NetThing();
                thing.Read(buffer);
                OfferedThings.Add(thing);
            }

            int requestedCount = buffer.ReadInt();
            for (int i = 0; i < requestedCount; i++)
            {
                var thing = new NetThing();
                thing.Read(buffer);
                RequestedThings.Add(thing);
            }

            CountOffered = buffer.ReadInt();
        }
    }
}
