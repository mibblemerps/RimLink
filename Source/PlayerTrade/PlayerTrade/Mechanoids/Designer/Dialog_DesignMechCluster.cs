using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Mechanoids.Designer
{
    public class Dialog_DesignMechCluster : Window
    {
        public Player Player;

        public override Vector2 InitialSize => new Vector2(640f, 620f);

        public MechCluster MechCluster = new MechCluster();

        private int _playerSilver;
        private Vector2 _scrollPosition = Vector2.zero;

        public Dialog_DesignMechCluster(Player player)
        {
            Player = player;

            doCloseX = true;
            forcePause = true;
            
            _playerSilver = LaunchUtil.LaunchableThingCount(Find.CurrentMap, ThingDefOf.Silver);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            inRect = inRect.AtZero();

            Rect headerRect = inRect.TopPartPixels(35f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Create Mechanoid Cluster");
            Text.Font = GameFont.Small;

            Rect addPartsRect = new Rect(0, 40f, inRect.width, 35f);
            if (Widgets.ButtonText(addPartsRect.LeftPart(0.5f), "Add Building"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var part in MechParts.Parts.Where(p => p.Type == MechPart.PartType.Building))
                    options.Add(new FloatMenuOption(AddPriceToLabel(part.ThingDef.LabelCap, part.BasePrice), () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.ButtonText(addPartsRect.RightPart(0.5f), "Add Creature"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var part in MechParts.Parts.Where(p => p.Type == MechPart.PartType.Pawn))
                    options.Add(new FloatMenuOption(AddPriceToLabel(part.PawnKindDef.LabelCap, part.BasePrice), () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect partsRect = new Rect(0, addPartsRect.yMax + 10f, inRect.width, inRect.height - addPartsRect.yMax - 10f - 45f);

            DrawParts(partsRect);

            Rect bottomRect = inRect.BottomPartPixels(35f);
            
            // Draw total price
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bottomRect.LeftPart(0.33f).CenteredOnXIn(bottomRect),
                ("$" + Mathf.RoundToInt(MechCluster.Price))
                .Colorize(_playerSilver >= Mathf.RoundToInt(MechCluster.Price) ? ColoredText.CurrencyColor : ColoredText.RedReadable));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            
            // Draw buttons
            if (Widgets.ButtonText(bottomRect.RightPart(0.33f), "Send Mech Cluster"))
                SendMechCluster();
            if (Widgets.ButtonText(bottomRect.LeftPart(0.33f), "Cancel"))
                Close();

            GUI.EndGroup();
        }

        public void AddPart(MechPart part)
        {
            MechCluster.Parts.Add(part.CreateConfig());
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
                error = "You must add at least 1 part to the mechanoid cluster.";

            if (!MechCluster.Parts.Any(part => part.MechPart.IsThreat))
                error = "Your mechanoid cluster must contain at least 1 threat.";

            if (MechCluster.Parts.Count > 50)
                error = "Your mechanoid cluster has too many parts.";

            if (!TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, Mathf.RoundToInt(MechCluster.Price)))
                error = "You don't have enough silver. Ensure your silver is located near a powered trade beacon.";

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
                    "This mechanoid cluster doesn't have any activators.\n" +
                    "The mechanoids will only wake when directly attacked.\n\n" +
                    "Continue with no activators?",
                    "Continue", () => {SendMechCluster(true);},
                    "Cancel", () => {}));
                return;
            }
            
            // Send cluster
            RimLinkComp.Instance.Client.SendPacket(new PacketMechanoidCluster
            {
                Cluster = MechCluster,
                For = Player.Guid,
                From = RimLinkComp.Instance.Guid
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
