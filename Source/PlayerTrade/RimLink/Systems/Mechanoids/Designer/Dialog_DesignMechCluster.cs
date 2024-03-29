﻿using System.Collections.Generic;
using System.Linq;
using RimLink.Core;
using RimLink.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Systems.Mechanoids.Designer
{
    public class Dialog_DesignMechCluster : Window
    {
        public Player Player;

        public override Vector2 InitialSize => new Vector2(640f, 620f);

        public MechCluster MechCluster = new MechCluster();

        protected float MechanoidDiscount => MechanoidSystem.GetDiscount();

        private int _playerSilver;
        private Vector2 _scrollPosition = Vector2.zero;

        public Dialog_DesignMechCluster(Player player)
        {
            Player = player;

            doCloseX = true;
            forcePause = true;
            
            _playerSilver = LaunchUtil.LaunchableThingCount(Find.CurrentMap, ThingDefOf.Silver);
            MechCluster.DiscountPercent = MechanoidDiscount;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            inRect = inRect.AtZero();

            // Header
            Rect headerRect = inRect.TopPartPixels(35f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Create Mechanoid Cluster");
            Text.Font = GameFont.Small;

            if (MechanoidDiscount > 0f)
            {
                // Discount text
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(headerRect.RightPart(0.5f), $"{Mathf.RoundToInt(MechanoidDiscount * 100f)}% discount".Colorize(ColoredText.CurrencyColor));
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            
            // Add buttons
            Rect addPartsRect = new Rect(0, 40f, inRect.width, 35f);
            if (Widgets.ButtonText(addPartsRect.LeftPart(0.5f), "Add Building"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var part in MechParts.Parts.Where(p => p.Type == MechPart.PartType.Building))
                    options.Add(new FloatMenuOption(AddPriceToLabel(part.ThingDef.LabelCap, Mathf.RoundToInt(part.BasePrice * (1f - MechanoidDiscount))), () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.ButtonText(addPartsRect.RightPart(0.5f), "Add Creature"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var part in MechParts.Parts.Where(p => p.Type == MechPart.PartType.Pawn))
                    options.Add(new FloatMenuOption(AddPriceToLabel(part.PawnKindDef.LabelCap, part.BasePrice), () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Mech parts
            Rect partsRect = new Rect(0, addPartsRect.yMax + 10f, inRect.width, inRect.height - addPartsRect.yMax - 10f - 50f);
            DrawParts(partsRect);

            Rect bottomRect = inRect.BottomPartPixels(35f);
            
            // Draw total price
            Rect priceAreaRect = bottomRect.LeftPart(0.33f).CenteredOnXIn(bottomRect);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(priceAreaRect.TopPartPixels(30f),
                ("$" + Mathf.RoundToInt(MechCluster.Price))
                .Colorize(_playerSilver >= Mathf.RoundToInt(MechCluster.Price) ? ColoredText.CurrencyColor : ColoredText.WarningColor));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Draw buttons
            if (Widgets.ButtonText(bottomRect.RightPart(0.33f), "Send Mech Cluster"))
                SendMechCluster();
            if (Widgets.ButtonText(bottomRect.LeftPart(0.33f), "Cancel"))
                Close();

            GUI.EndGroup();
        }

        public void AddPart(MechPart part)
        {
            var config = part.CreateConfig();
            config.DiscountPercent = MechanoidDiscount;
            MechCluster.Parts.Add(config);
        }

        private void DrawParts(Rect rect)
        {
            Rect viewRect = new Rect(0, 0, rect.width - 20f, MechCluster.Parts.Count * 35f);
            _scrollPosition = GUI.BeginScrollView(rect, _scrollPosition, viewRect);

            // Draw parts
            var toRemove = new List<MechPartConfig>();
            int i = 0;
            foreach (MechPartConfig config in MechCluster.Parts)
            {
                Rect partRect = new Rect(0, 35f * i, rect.width, 35f);
                if (i % 2 == 0)
                    Widgets.DrawHighlight(partRect); // Draw row highlight every second row
                config.Draw(partRect);
                if (config.Remove)
                    toRemove.Add(config);
                i++;
            }

            GUI.EndScrollView();

            // Remove any parts pending removal
            foreach (MechPartConfig config in toRemove)
                MechCluster.Parts.Remove(config);
        }

        private void SendMechCluster(bool ignoreMissingActivator = false)
        {
            // Sanity check
            string error = null;
            if (MechCluster.Parts.Count == 0)
                error = "Rl_MechClusterMustHave1Part".Translate();

            if (!MechCluster.Parts.Any(part => part.MechPart.IsBuildingThreat))
                error = "Rl_MechClusterMustContain1Threat".Translate();

            if (MechCluster.Parts.Count > 50)
                error = "Rl_MechClusterHasTooManyParts".Translate();

            if (!TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, Mathf.RoundToInt(MechCluster.Price)))
                error = "Rl_MechClusterNotEnoughSilver".Translate();

            if (error != null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(error));
                return;
            }
            
            // Ensure cluster has an activator
            if (!ignoreMissingActivator && !MechCluster.Parts.Any(config => config.MechPart.Type == MechPart.PartType.Building &&
                                                                            config.MechPart.ThingDef.building.buildingTags.Contains("MechClusterActivator")))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Rl_DialogMechClusterHasNoActivators".Translate(),
                    "Continue", () => {SendMechCluster(true);},
                    "Cancel", () => {}));
                return;
            }
            
            // Send cluster
            RimLink.Instance.Client.SendPacket(new PacketMechanoidCluster
            {
                Cluster = MechCluster,
                For = Player.Guid,
                From = RimLink.Instance.Guid
            });
            
            // Pay monies
            TradeUtility.LaunchSilver(Find.CurrentMap, Mathf.RoundToInt(MechCluster.Price));
            
            Messages.Message($"The mechanoids will deploy a cluster to {Player.Name} according to your specifications.", MessageTypeDefOf.PositiveEvent);
            Close();
        }

        private string AddPriceToLabel(string label, float price)
        {
            const float desiredWidth = 130;

            float spaceWidth = Text.CalcSize("\t").x;
            float labelWidth = Text.CalcSize(label).x;

            string padding = "";
            if (labelWidth < desiredWidth)
            {
                int spaces = Mathf.CeilToInt((desiredWidth - labelWidth) / spaceWidth);
                padding = new string('\t', spaces);
            }

            return $"{label}{padding}\t<color=#6e6e6e>${price}</color>";
        }
    }
}
