using System;
using System.Collections.Generic;
using RimLink.Util;
using RimLink.Net.Packets;
using RimLink.Systems.Trade;
using Verse;

namespace RimLink.Net
{
    [Serializable]
    public class NetTradeThing : IPacketable
    {
        public bool IsPawn;

        public List<NetThing> OfferedThings = new List<NetThing>();
        public List<NetThing> RequestedThings = new List<NetThing>();

        public List<NetHuman> OfferedPawns = new List<NetHuman>();
        public List<NetHuman> RequestedPawns = new List<NetHuman>();

        public int CountOffered;

        public TradeOffer.TradeThing ToTradeThing()
        {
            var offered = new List<Thing>();
            var requested = new List<Thing>();
            if (IsPawn)
            {
                foreach (NetHuman pawn in OfferedPawns)
                    offered.Add(pawn.ToPawn());
                foreach (NetHuman pawn in RequestedPawns)
                    requested.Add(pawn.ToPawn());
            }
            else
            {
                foreach (NetThing netThing in OfferedThings)
                    offered.Add(netThing.ToThing());
                foreach (NetThing netThing in RequestedThings)
                    requested.Add(netThing.ToThing());
            }

            return new TradeOffer.TradeThing(offered, requested, CountOffered);
        }

        public static NetTradeThing FromTradeThing(TradeOffer.TradeThing tradeThing)
        {
            var net = new NetTradeThing();
            net.IsPawn = tradeThing.IsPawn;
            net.CountOffered = tradeThing.CountOffered;

            if (tradeThing.IsPawn)
            {
                foreach (Thing thing in tradeThing.RequestedThings)
                {
                    if (!(thing is Pawn pawn))
                    {
                        Log.Warn("Tradeable is supposed to be pawns but it's not");
                        continue;
                    }
                    
                    net.RequestedPawns.Add(pawn.ToNetHuman());
                }

                foreach (Thing thing in tradeThing.OfferedThings)
                {
                    if (!(thing is Pawn pawn))
                    {
                        Log.Warn("Tradeable is supposed to be pawns but it's not");
                        continue;
                    }

                    net.OfferedPawns.Add(pawn.ToNetHuman());
                }
            }
            else
            {
                foreach (Thing thing in tradeThing.RequestedThings)
                    net.RequestedThings.Add(NetThing.FromThing(thing));
                foreach (Thing thing in tradeThing.OfferedThings)
                    net.OfferedThings.Add(NetThing.FromThing(thing));
            }

            return net;
        }

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteBoolean(IsPawn);

            if (IsPawn)
            {
                buffer.WriteInt(OfferedPawns.Count);
                foreach (NetHuman pawn in OfferedPawns)
                    buffer.WritePacketable(pawn);

                buffer.WriteInt(RequestedPawns.Count);
                foreach (NetHuman pawn in RequestedPawns)
                    buffer.WritePacketable(pawn);
            }
            else
            {
                buffer.WriteInt(OfferedThings.Count);
                foreach (NetThing thing in OfferedThings)
                    thing.Write(buffer);

                buffer.WriteInt(RequestedThings.Count);
                foreach (NetThing thing in RequestedThings)
                    thing.Write(buffer);
            }

            buffer.WriteInt(CountOffered);
        }

        public void Read(PacketBuffer buffer)
        {
            IsPawn = buffer.ReadBoolean();

            if (IsPawn)
            {
                int offeredCount = buffer.ReadInt();
                for (int i = 0; i < offeredCount; i++)
                {
                    OfferedPawns.Add(buffer.ReadPacketable<NetHuman>());
                }

                int requestedCount = buffer.ReadInt();
                for (int i = 0; i < requestedCount; i++)
                {
                    RequestedPawns.Add(buffer.ReadPacketable<NetHuman>());
                }
            }
            else
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
            }

            CountOffered = buffer.ReadInt();
        }
    }
}
