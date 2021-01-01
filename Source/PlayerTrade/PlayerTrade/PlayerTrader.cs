﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
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

        private List<Thing> _goodsCache = new List<Thing>();

        public PlayerTrader(Player player, Resources resources)
        {
            Player = player;

            TraderKind = DefDatabase<TraderKindDef>.GetNamed("PlayerTrader");
            Faction = Faction.OfPlayer;

            _goodsCache = new List<Thing>();
            foreach (NetThing netThing in resources.Things)
                _goodsCache.Add(netThing.ToThing());
        }

        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(playerNegotiator.Map, this))
                yield return thing;
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
