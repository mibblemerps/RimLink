using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PlayerTrade
{
    public class PlayerFactions : IExposable
    {
        public RimLinkComp RimLinkComp;

        private List<PlayerFactionData> FactionData;

        public PlayerFactions(RimLinkComp rimLinkComp)
        {
            RimLinkComp = rimLinkComp;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref FactionData, "faction_data", LookMode.Deep);
        }

        private class PlayerFactionData : IExposable
        {
            public string Guid;
            public Faction Faction;

            public void ExposeData()
            {
                Scribe_Values.Look(ref Guid, "guid");
                Scribe_References.Look(ref Faction, "faction");
            }
        }
    }
}
