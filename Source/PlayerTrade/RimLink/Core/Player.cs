using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Util;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Systems.Trade;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Core
{
    [Serializable]
    public class Player : IExposable, IPacketable
    {
        public string Guid;
        public string Name;
        public float[] Color = ColoredText.FactionColor_Neutral.ToFloats();

        public List<Colony> Colonies;
        
        public int Wealth;
        public int Day;
        public string Weather;
        public int Temperature = int.MinValue;
        public bool TradeableNow;

        public List<Faction> LocalFactions;

        public bool IsUs => Guid == RimLink.Instance.Guid;
        public bool IsOnline => RimLink.Instance.Client.OnlinePlayers.ContainsKey(Guid);

        public Player(string guid)
        {
            Guid = guid;
        }

        public Player()
        {
        }

        public static Player Self(bool mapIndependent = false)
        {
            RimLink comp = RimLink.Instance;
            var player = new Player(comp.Guid);
            player.Name = RimWorld.Faction.OfPlayer.Name;
            player.TradeableNow = CommsConsoleUtility.PlayerHasPoweredCommsConsole();
            player.Wealth = Mathf.RoundToInt(TradeUtil.TotalWealth());
            player.Day = Mathf.FloorToInt(Current.Game.tickManager.TicksGame / 60000f);
            if (!mapIndependent && Find.CurrentMap != null)
            {
                player.Weather = Find.CurrentMap.weatherManager.curWeather.defName;
                player.Temperature = Mathf.RoundToInt(Find.CurrentMap.mapTemperature.OutdoorTemp);
                
                // Colonies
                player.Colonies = new List<Colony>();
                foreach (var settlement in Find.WorldObjects.SettlementBases
                    .Where(settlement => settlement.Faction == RimWorld.Faction.OfPlayer))
                {
                    player.Colonies.Add(new Colony
                    {
                        Id = settlement.ID,
                        OwnerGuid = player.Guid,
                        Name = settlement.Name,
                        Seed = Find.World.info.seedString,
                        Tile = settlement.Map.Tile,
                    });
                }
            }

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

        public void Write(PacketBuffer buffer)
        {
            buffer.WriteString(Guid);
            buffer.WriteString(Name);
            buffer.WriteColor(Color.ToColor(), false);
            buffer.WriteInt(Wealth);
            buffer.WriteInt(Day);
            buffer.WriteString(Weather, true);
            buffer.WriteInt(Temperature);
            buffer.WriteBoolean(TradeableNow);

            buffer.WriteList(LocalFactions, (b, i) => b.WritePacketable(i));
        }

        public void Read(PacketBuffer buffer)
        {
            Guid = buffer.ReadString();
            Name = buffer.ReadString();
            Color = buffer.ReadColor(false).ToFloats();
            Wealth = buffer.ReadInt();
            Day = buffer.ReadInt();
            Weather = buffer.ReadString(true);
            Temperature = buffer.ReadInt();
            TradeableNow = buffer.ReadBoolean();

            LocalFactions = new List<Faction>();
            LocalFactions = buffer.ReadList<Faction>(b => b.ReadPacketable<Faction>());
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Name, "name");
            Scribe_Values.Look(ref Color[0], "color_r");
            Scribe_Values.Look(ref Color[1], "color_g");
            Scribe_Values.Look(ref Color[2], "color_b");
            Scribe_Collections.Look(ref LocalFactions, "local_factions");
        }

        public override string ToString()
        {
            return $"{Name} ({Guid.Substring(0, Math.Min(8, Guid.Length))})";
        }

        [Serializable]
        public class Faction : IExposable, IPacketable
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

            public void Write(PacketBuffer buffer)
            {
                buffer.WriteString(Name);
                buffer.WriteInt(Goodwill);
                buffer.WriteString(FactionDef);
                buffer.WriteFloat(FactionColorR);
                buffer.WriteFloat(FactionColorG);
                buffer.WriteFloat(FactionColorB);
            }

            public void Read(PacketBuffer buffer)
            {
                Name = buffer.ReadString();
                Goodwill = buffer.ReadInt();
                FactionDef = buffer.ReadString();
                FactionColorR = buffer.ReadFloat();
                FactionColorG = buffer.ReadFloat();
                FactionColorB = buffer.ReadFloat();
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref Name, "name");
                Scribe_Values.Look(ref Goodwill, "goodwill");
                Scribe_Values.Look(ref FactionDef, "faction_def");
                Scribe_Values.Look(ref FactionColorR, "color_r");
                Scribe_Values.Look(ref FactionColorG, "color_g");
                Scribe_Values.Look(ref FactionColorB, "color_b");
            }
        }
    }
}
