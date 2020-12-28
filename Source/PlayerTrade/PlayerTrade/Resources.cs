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
    public class Resources
    {
        public List<NetThing> Things = new List<NetThing>();
        //public List<Pawn> Pawns = new List<Pawn>(); // todo: animals and prisoners

        public float CalculateMarketValue()
        {
            float marketValue = 0f;
            foreach (NetThing thing in Things)
            {
                // todo: fix
            }
            return marketValue;
        }

        public void Write(PacketBuffer buffer)
        {
            // Write things
            buffer.WriteInt(Things.Count);
            foreach (NetThing netThing in Things)
            {
                netThing.Write(buffer);
            }
        }

        public void Read(PacketBuffer buffer)
        {
            Things.Clear();

            // Read things
            int thingsCount = buffer.ReadInt();
            for (int i = 0; i < thingsCount; i++)
            {
                var newThing = new NetThing();
                newThing.Read(buffer);
                Things.Add(newThing);
            }
        }

        public void Update(Map map)
        {
            Things.Clear();

            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(map))
            {
                Things.Add(NetThing.FromThing(thing));
            }

            // Old code for getting all resources, rather than ones within range of a trade beacon
            /*List<SlotGroup> groupsListForReading = map.haulDestinationManager.AllGroupsListForReading;
            foreach (SlotGroup slotGroup in groupsListForReading)
            {
                foreach (Thing thing in slotGroup.HeldThings)
                {
                    Things.Add(NetThing.FromThing(thing));
                }
            }*/
        }
    }
}
