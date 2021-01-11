using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Trade;
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
        public int Day;
        public string Weather;
        public int Temperature;
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
                TradeableNow = CommsConsoleUtility.PlayerHasPoweredCommsConsole(),
                Wealth = Mathf.RoundToInt(TradeUtil.TotalWealth()),
                Day = Mathf.FloorToInt(Current.Game.tickManager.TicksGame / 60000f),
                Weather = Find.CurrentMap.weatherManager.curWeather.defName,
                Temperature = Mathf.RoundToInt(Find.CurrentMap.mapTemperature.OutdoorTemp),
            };

            // Populate factions
            player.LocalFactions = new List<Faction>();
            foreach (var faction in Find.FactionManager.AllFactionsVisibleInViewOrder)
            {
                if (faction.Hidden || faction.defeated || faction.IsPlayer)
                    continue;

                player.LocalFactions.Add(new Faction
                {
                    Name = faction.Name,
                    Goodwill = faction.PlayerGoodwill,
                    FactionDef = faction.def.defName,
                    FactionColor = faction.Color,
                });
            }

            return player;
        }

        [Serializable]
        public class Faction
        {
            public string Name;
            public int Goodwill;
            public string FactionDef;
            // We have these because the Unity color type cannot be serialized :c
            public float FactionColorR;
            public float FactionColorG;
            public float FactionColorB;

            public Color FactionColor
            {
                get => new Color(FactionColorR, FactionColorG, FactionColorB);
                set
                {
                    FactionColorR = value.r;
                    FactionColorB = value.b;
                    FactionColorG = value.g;
                }
            }
            public bool CanUseDropPods => FindDef().techLevel >= TechLevel.Industrial;

            public FactionDef FindDef()
            {
                return RimWorld.FactionDef.Named(FactionDef);
            }
        }
    }
}
