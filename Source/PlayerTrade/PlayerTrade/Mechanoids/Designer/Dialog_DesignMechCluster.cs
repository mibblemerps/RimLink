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

        private Vector2 _scrollPosition = Vector2.zero;

        private static List<MechPart> AvailableParts = new List<MechPart>
        {
            // Buildings
            new MechPart(DefDatabase<ThingDef>.GetNamed("Turret_AutoChargeBlaster"), 600),
            new MechPart(ThingDefOf.Turret_AutoMiniTurret, 200),
            new MechPart(ThingDefOf.ShieldGeneratorMortar, 400),
            new MechPart(ThingDefOf.ShieldGeneratorBullets, 400),
            new MechPart(ThingDefOf.ActivatorCountdown, 200, typeof(MechPartConfigCountdownActivator)),
            new MechPart(ThingDefOf.ActivatorProximity, 150, typeof(MechPartConfigProximityActivator)),

            // Creatures
            new MechPart(DefDatabase<ThingDef>.GetNamed("Mech_Centipede"), 600),
            new MechPart(DefDatabase<ThingDef>.GetNamed("Mech_Lancer"), 300),
            new MechPart(DefDatabase<ThingDef>.GetNamed("Mech_Scyther"), 300),
            new MechPart(DefDatabase<ThingDef>.GetNamed("Mech_Pikeman"), 300),
        };

        public Dialog_DesignMechCluster(Player player)
        {
            Player = player;

            doCloseX = true;
            forcePause = true;
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
                foreach (var part in AvailableParts.Where(p => p.ThingDef.category == ThingCategory.Building))
                    options.Add(new FloatMenuOption(part.ThingDef.LabelCap, () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.ButtonText(addPartsRect.RightPart(0.5f), "Add Creature"))
            {
                var options = new List<FloatMenuOption>();
                foreach (var part in AvailableParts.Where(p => p.ThingDef.category != ThingCategory.Building))
                    options.Add(new FloatMenuOption(part.ThingDef.LabelCap, () => { AddPart(part); }, part.Icon, Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect partsRect = new Rect(0, addPartsRect.yMax + 10f, inRect.width, inRect.height - addPartsRect.yMax - 10f - 45f);

            DrawParts(partsRect);

            Rect buttonsRect = inRect.BottomPartPixels(35f).RightHalf();
            if (Widgets.ButtonText(buttonsRect.RightPart(0.45f), "Send Mech Cluster"))
            {
                // todo: send cluster
            }
            if (Widgets.ButtonText(buttonsRect.LeftPart(0.45f), "Cancel"))
            {
                Close();
            }

            GUI.EndGroup();
        }

        public void AddPart(MechPart part)
        {
            MechCluster.Parts.Add(part.CreateConfig(MechCluster));
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
    }
}
