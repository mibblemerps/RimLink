using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimLink.Systems.Trade
{
    public class StockGenerator_PlayerBuys : StockGenerator
    {
        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            return new Thing[0];
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.tradeability != Tradeability.None && !thingDef.isUnfinishedThing;
        }
    }
}
