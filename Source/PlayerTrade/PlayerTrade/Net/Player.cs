using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class Player
    {
        public readonly string Guid;
        public string Name;
        public bool TradeableNow;

        public List<string> LocalFactions;

        public Player(string guid)
        {
            Guid = guid;
        }

        public static Player Self()
        {
            RimLinkGameComponent comp = RimLinkGameComponent.Find();
            var player = new Player(comp.Guid)
            {
                Name = Faction.OfPlayer.Name,
                TradeableNow = true // todo: implement properly
            };

            return player;
        }
    }
}
