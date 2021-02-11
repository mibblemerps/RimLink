using System.Collections.Generic;
using System.Linq;
using PlayerTrade.Net;
using PlayerTrade.Patches;
using RimWorld;
using Verse;

namespace PlayerTrade.Trade
{
    public class PlayerTrader : ITrader
    {
        public Player Player;

        public Dictionary<ThingDef, int> Counts = new Dictionary<ThingDef, int>();

        public TraderKindDef TraderKind { get; }

        public IEnumerable<Thing> Goods => _goodsCache;

        public int RandomPriceFactorSeed => 0;
        public string TraderName => Player.Name;
        public bool CanTradeNow => true;
        public float TradePriceImprovementOffsetForPlayer => 0f;
        public Faction Faction { get; }
        public TradeCurrency TradeCurrency => TradeCurrency.Silver;

        private List<Thing> _goodsCache;

        public PlayerTrader(Player player, Resources resources)
        {
            Player = player;

            TraderKind = DefDatabase<TraderKindDef>.GetNamed("PlayerTrader");
            Faction = Faction.OfPlayer;

            _goodsCache = new List<Thing>();
            foreach (NetThing netThing in resources.Things)
                _goodsCache.Add(netThing.ToThing());
            foreach (NetHuman netHuman in resources.Pawns)
                _goodsCache.Add(netHuman.ToPawn());
        }

        // Things that the other player is "willing" to buy
        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            bool wasOn = Patch_TradeUtility_EverPlayerSellable.ForceEnable;
            if (!wasOn)
                Patch_TradeUtility_EverPlayerSellable.ForceEnable = true;

            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(playerNegotiator.Map, this))
                yield return thing;
            foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(playerNegotiator.Map).Where(p => p.RaceProps.Humanlike))
                yield return pawn;

            if (!wasOn)
                Patch_TradeUtility_EverPlayerSellable.ForceEnable = false;
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            //
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            //
        }
    }
}
