using System;
using System.Collections.Generic;
using System.Linq;
using RimLink.Core;
using RimLink.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimLink.Systems.Raids
{
    public class Dialog_PlaceBounty : Window
    {
        public const float DefaultBasePrice = 1000f;
        public const int DefaultMaxStrengthPercent = 500;

        public Player Player;

        public override Vector2 InitialSize => new Vector2(512f, 512f);

        protected float TribalDiscount => RaidSystem.GetDiscount();

        private int _playerSilver;

        private Player.Faction _selectedFaction;
        private float _strength = 100f;
        private Strategy _strategy = _strategies[1];
        private ArrivalMethod _arrivalMode = _arrivalMethods[0];
        private ArrivalSpeed _arrivalSpeed = _arrivalSpeeds[2];

        private static List<ArrivalMethod> _arrivalMethods = new List<ArrivalMethod>
        {
            new ArrivalMethod(PawnsArrivalModeDefOf.EdgeWalkIn, 0f, "Walk In"),
            new ArrivalMethod(PawnsArrivalModeDefOf.EdgeDrop, 0.04f, "Edge Drop Pods", true),
            new ArrivalMethod(PawnsArrivalModeDefOf.RandomDrop, 0.33f, "Random Drop Pods", true),
            new ArrivalMethod(PawnsArrivalModeDefOf.CenterDrop, 0.9f, "Center Drop Pods", true),
        };

        private static List<ArrivalSpeed> _arrivalSpeeds = new List<ArrivalSpeed>
        {
            new ArrivalSpeed(0, 0.25f, "Immediate", true),
            new ArrivalSpeed(2f, 0.1f, "2 hours", true),
            new ArrivalSpeed(24f, 0f, "1 day"),
            new ArrivalSpeed(48f, -0.1f, "2 days"),
            new ArrivalSpeed(72f, -0.125f, "3 days"),
        };

        private static List<Strategy> _strategies = new List<Strategy>
        {
            new Strategy("StageThenAttack", "Prepare Then Attack", -0.1f),
            new Strategy("ImmediateAttack", "Immediate Attack", 0f),
            new Strategy("ImmediateAttackSmart", "Immediate Attack Smart", 0.15f),
            new Strategy("ImmediateAttackSappers", "Immediate Attack Sappers", 0.36f),
            new Strategy("Siege", "Siege", 0.66f),
        };

        public Dialog_PlaceBounty(Player player)
        {
            Player = player;
            _selectedFaction = player.LocalFactions.FirstOrDefault(faction => faction.Goodwill < 0f);
            doCloseX = true;
            forcePause = true;

            _playerSilver = LaunchUtil.LaunchableThingCount(Find.CurrentMap, ThingDefOf.Silver);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (_selectedFaction == null)
            {
                Log.Error("Player has no factions that are hostile. This shouldn't happen in an unmodified game.");
                Close();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);

            Text.Font = GameFont.Medium;
            string label1 = $"Place Bounty on {(Player.Name.Colorize(ColoredText.FactionColor_Neutral))}";
            Rect rect1 = new Rect(0, 0, Text.CalcSize(label1).x, 50f);
            Widgets.Label(rect1, label1);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            string label2 = "Faction to place bounty with:";
            Rect rect2 = new Rect(0, rect1.yMax + 35f, Text.CalcSize(label2).x, Text.CalcSize(label2).y);
            Widgets.Label(rect2, label2);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(new Rect(rect2.xMax + 10f, rect2.yMin, 220, 35), _selectedFaction == null ? "Select Faction" : _selectedFaction.Name))
            {
                var options = new List<FloatMenuOption>();
                foreach (Player.Faction faction in Player.LocalFactions)
                {
                    FactionDef factionDef = faction.FindDef();

                    var option = new FloatMenuOption($"{faction.Name} ({faction.Goodwill})", () =>
                    {
                        _selectedFaction = faction;
                        ResolveIncompatibilities();
                    }, factionDef.FactionIcon, faction.FactionColor);
                    if (faction.Goodwill >= 0)
                    {
                        // Allied or neutral factions cannot be used for bounties
                        option.Label += (faction.Goodwill >= 75) ? " (ally)" : " (neutral)";
                        //option.Disabled = true;
                        continue; // just hide neutral/ally factions
                    }
                    else
                    {
                        // Show tribal discount if applicable
                        if (TribalDiscount > 0f && faction.FindDef().techLevel < TechLevel.Industrial)
                        {
                            option.Label += $" <color=\"#6e6e6e\">({Mathf.RoundToInt(TribalDiscount * 100)}% discount)</color>";
                        }
                    }
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect stratRect = new Rect(0, rect2.yMax + 20f, inRect.width, 35f);
            if (Widgets.ButtonText(stratRect.LeftPartPixels(220f), _strategy.Label))
            {
                var options = new List<FloatMenuOption>();
                foreach (Strategy strat in GetAllowableStrategies(false))
                {
                    var option = new FloatMenuOption(strat.Label, () =>
                    {
                        _strategy = strat;
                        ResolveIncompatibilities();
                    });

                    // Tribals cannot do sieges
                    if (strat.Def.defName == "Siege" && _selectedFaction.FindDef().techLevel < TechLevel.Industrial)
                    {
                        option.Disabled = true;
                        option.Label += " (insufficient technology)";
                    }
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect strengthSliderRect = new Rect(0, stratRect.yMax + 10f, inRect.width, 35f);
            _strength = Widgets.HorizontalSlider(strengthSliderRect, _strength, 20, RimLink.Instance.Client.LegacySettings.RaidMaxStrengthPercent, true, $"Raid Strength ({Mathf.RoundToInt(_strength)}%)", roundTo: 1f);

            Rect dropdownsRect = new Rect(0, strengthSliderRect.yMax + 10f, inRect.width, 35f);
            // Arrival mode
            if (Widgets.ButtonText(dropdownsRect.LeftPartPixels(220f), _arrivalMode.Label))
            {
                var options = new List<FloatMenuOption>();
                foreach (ArrivalMethod method in GetAllowableArrivalMethods(false))
                {
                    var option = new FloatMenuOption(method.Label, () =>
                    {
                        _arrivalMode = method;
                        ResolveIncompatibilities();
                    });
                    if (!_selectedFaction.CanUseDropPods && method.IsDropPods)
                    {
                        option.Disabled = true;
                        option.Label += " (insufficient technology)";
                    }
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            // Arrival speed
            if (Widgets.ButtonText(dropdownsRect.RightPartPixels(220f), _arrivalSpeed.Label))
            {
                var options = new List<FloatMenuOption>();
                foreach (ArrivalSpeed speed in GetAllowableArrivalSpeeds(false))
                {
                    var option = new FloatMenuOption(speed.Label, () => { _arrivalSpeed = speed; });
                    if (!_arrivalMode.IsDropPods && speed.RequiresDropPods)
                    {
                        option.Disabled = true;
                        option.Label += " (requires drop pods)";
                    }
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            int cost = CalculateCost();
            bool insufficientSilver = cost > _playerSilver;
            Rect costRect = new Rect(0, dropdownsRect.yMax + 10f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Widgets.Label(costRect, "Cost: " + ("$" + cost).Colorize(insufficientSilver ? ColoredText.RedReadable : ColoredText.CurrencyColor));
            
            Text.Font = GameFont.Tiny;
            Rect costExtraRect = new Rect(0, costRect.yMax + 5f, inRect.width, 30f);
            string costExtraText = "";
            if (insufficientSilver)
                costExtraText += "Insufficient silver\n";
            if (TribalDiscount > 0 && _selectedFaction.FindDef().techLevel < TechLevel.Industrial)
                costExtraText += $"{Mathf.RoundToInt(TribalDiscount * 100)}% discount for tribal";
            Widgets.Label(costExtraRect, costExtraText);
            Text.Font = GameFont.Small;

            Rect sendRect = inRect.BottomPartPixels(35f).RightPartPixels(200f);
            sendRect.position -= new Vector2(10f, 10f);

            if (Widgets.ButtonText(sendRect, "Place Bounty", active: !insufficientSilver))
            {
                // Double check silver incase it somehow changed while paused
                if (!TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, cost))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("Ensure silver is located around a powered orbital trade beacon.", "Close", title: "Insufficient Silver"));
                    return;
                }

                Log.Message("Send bounty! Cost: " + cost);
                TradeUtility.LaunchSilver(Find.CurrentMap, cost);

                var raid = CreateRaidOptions();
                raid.Send(Player.Guid).ContinueWith((t) =>
                {
                    Messages.Message($"Bounty placed against {Player.Name}. {_selectedFaction.Name} will handle the matter {(_arrivalSpeed.Hours < 24 ? "shortly" : "soon")}.", MessageTypeDefOf.NeutralEvent);
                });
                
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                Close();
            }

            GUI.EndGroup();
        }

        private IEnumerable<ArrivalMethod> GetAllowableArrivalMethods(bool hideTechnologicallyIncapable)
        {
            foreach (ArrivalMethod method in _arrivalMethods)
            {
                if (hideTechnologicallyIncapable && method.IsDropPods && !_selectedFaction.CanUseDropPods)
                    continue;
                if (!_strategy.Def.arriveModes.Contains(method.Def))
                    continue;
                yield return method;
            }
        }

        private IEnumerable<ArrivalSpeed> GetAllowableArrivalSpeeds(bool hideTechnologicallyIncapable)
        {
            foreach (ArrivalSpeed speed in _arrivalSpeeds)
            {
                if (hideTechnologicallyIncapable && speed.RequiresDropPods && !_arrivalMode.IsDropPods)
                    continue;
                yield return speed;
            }
        }

        private IEnumerable<Strategy> GetAllowableStrategies(bool hideTechnologicallyIncapable)
        {
            foreach (Strategy strat in _strategies)
            {
                if (hideTechnologicallyIncapable && _selectedFaction.FindDef().techLevel < TechLevel.Industrial && strat.DefName == "Siege")
                    continue;
                yield return strat;
            }
        }

        private void ResolveIncompatibilities()
        {
            if (!GetAllowableStrategies(true).Contains(_strategy))
            {
                // Strategy not allowable
                _strategy = GetAllowableStrategies(true).First();
            }

            if (!GetAllowableArrivalMethods(true).Contains(_arrivalMode))
            {
                // Arrival method not allowable
                _arrivalMode = GetAllowableArrivalMethods(true).First();
            }

            if (!GetAllowableArrivalSpeeds(true).Contains(_arrivalSpeed))
            {
                // Arrival speed not allowable
                _arrivalSpeed = GetAllowableArrivalSpeeds(true).First();
            }
        }

        private int CalculateCost()
        {
            float cost = (_strength / 100f) * RimLink.Instance.Client.LegacySettings.RaidBasePrice;
            float multiplier = 1f;
            multiplier += _strategy.Cost;
            multiplier += _arrivalMode.Cost;
            multiplier += _arrivalSpeed.Cost;

            cost = cost * multiplier;

            if (_selectedFaction.FindDef().techLevel < TechLevel.Industrial)
                cost *= 1f - TribalDiscount;

            return Mathf.RoundToInt(cost);
        }

        private BountyRaid CreateRaidOptions()
        {
            var raid = new BountyRaid
            {
                Id = Guid.NewGuid().ToString(),
                From = RimLink.Instance.Guid,
                ArrivalMode = _arrivalMode.Def,
                Strategy = _strategy.DefName,
                Strength = _strength / 100f,
                FactionName = _selectedFaction.Name,
                ArrivesInTicks = Mathf.RoundToInt(_arrivalSpeed.Hours * 2500f),
            };
            return raid;
        }

        private class ArrivalMethod
        {
            public PawnsArrivalModeDef Def;
            public float Cost;
            public string Label;
            public bool IsDropPods;

            public ArrivalMethod(PawnsArrivalModeDef def, float cost, string label, bool isDropPods = false)
            {
                Def = def;
                Cost = cost;
                Label = label;
                IsDropPods = isDropPods;
            }
        }

        private class ArrivalSpeed
        {
            public float Hours;
            public float Cost;
            public string Label;
            public bool RequiresDropPods;

            public ArrivalSpeed(float hours, float cost, string label, bool requiresDropPods = false)
            {
                Hours = hours;
                Cost = cost;
                Label = label;
                RequiresDropPods = requiresDropPods;
            }
        }

        private class Strategy
        {
            public string DefName;
            public string Label;
            public float Cost;

            public RaidStrategyDef Def => DefDatabase<RaidStrategyDef>.GetNamed(DefName);

            public Strategy(string defName, string label, float cost)
            {
                DefName = defName;
                Cost = cost;
                Label = label;
            }
        }
    }
}
