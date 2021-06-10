using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimLink.Core
{
    public class PlayerFactions : IExposable
    {
        public RimLink RimLink;

        private List<PlayerFactionData> FactionData;

        public PlayerFactions(RimLink rimLink)
        {
            RimLink = rimLink;
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
