using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    [Serializable]
    public class Player
    {
        public readonly string Guid;
        public string Name;
        public int Wealth;
        public bool TradeableNow;

        public List<Faction> LocalFactions;

        public Player(string guid)
        {
            Guid = guid;
        }

        public static Player Self()
        {
            RimLinkComp comp = RimLinkComp.Find();
            var player = new Player(comp.Guid)
            {
                Name = RimWorld.Faction.OfPlayer.Name,
                TradeableNow = true, // todo: implement properly
            };

            // Total wealth
            float totalWealth = 0f;
            foreach (Map map in Find.Maps)
                totalWealth += map.wealthWatcher.WealthTotal;
            player.Wealth = Mathf.RoundToInt(totalWealth);

            // Populate factions
            player.LocalFactions = new List<Faction>();
            foreach (var faction in Find.FactionManager.AllFactionsVisibleInViewOrder)
            {
                if (faction.Hidden || faction.defeated || faction.IsPlayer)
                    continue;

                player.LocalFactions.Add(new Faction
                {
                    Name = faction.Name,
                    Goodwill = faction.PlayerGoodwill
                });
            }

            return player;
        }

        [Serializable]
        public class Faction
        {
            public string Name;
            public int Goodwill;
        }
    }
}
