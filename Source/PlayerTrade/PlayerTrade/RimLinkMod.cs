using System;
using System.Reflection;
using HarmonyLib;
using PlayerTrade.Mechanoids;
using PlayerTrade.Net;
using PlayerTrade.Raids;
using PlayerTrade.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class RimLinkMod : Mod
    {
        public static readonly int ProtocolVersion = 6;

        public static RimLinkMod Instance { get; private set; }

        public ModSettings Settings;

        private static bool DoneInit = false;

        public static bool Active
        {
            get
            {
                if (RimLinkComp.Instance == null || RimLinkComp.Instance.Client == null)
                    return false;

                try
                {
                    return RimLinkComp.Instance.Client.State == Connection.ConnectionState.Authenticated;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public RimLinkMod(ModContentPack content) : base(content)
        {
            Instance = this;

            Settings = GetSettings<ModSettings>();

            // Initialize harmony
            var harmony = new Harmony("net.mitchfizz05.PlayerTrade");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Init()
        {
            if (DoneInit) return;
            DoneInit = true;

            ResearchUtil.AddUnlockableInfo(MechanoidSystem.ResearchProjectDefOf_Mechanoids.MechanoidCommunications,
                "Rl_AbilityToSendMechanoidClusters", ThingDef.Named("MechAssembler"));
            ResearchUtil.AddUnlockableInfo(MechanoidSystem.ResearchProjectDefOf_Mechanoids.MechanoidRelations,
                "Rl_MechanoidClusterDiscount", ThingDef.Named("MechAssembler"));

            ResearchUtil.AddUnlockableInfo(RaidSystem.ResearchProjectDefOf_Raid.NativeLanguages,
                "Rl_MinorTribalDiscount", ThingDef.Named("MeleeWeapon_Club"));
            ResearchUtil.AddUnlockableInfo(RaidSystem.ResearchProjectDefOf_Raid.NativeCulture,
                "Rl_MajorTribalDiscount", ThingDef.Named("MeleeWeapon_Club"));
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            inRect.width = Mathf.Min(inRect.width, 300);
            inRect = inRect.CenteredOnXIn(inRect);
            listing.Begin(inRect);

            if (listing.ButtonText("Set Server IP"))
                Find.WindowStack.Add(new Dialog_SetServerIp());

            listing.CheckboxLabeled("Logging Enabled", ref Settings.LoggingEnabled);
            Log.Enabled = Settings.LoggingEnabled;

            listing.CheckboxLabeled("Enable Main Menu Widget", ref Settings.MainMenuWidgetEnabled);
            
            listing.CheckboxLabeled("Enable Chat Notifications", ref Settings.ChatNotificationsEnabled);
            
            listing.CheckboxLabeled("Enable Pawn Import/Export Tool", ref Settings.ImportExportPawn);

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Player Trade";
        }

        public static void ShowModSettings()
        {
            var dialog = new Dialog_ModSettings();
            typeof(Dialog_ModSettings).GetField("selMod", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dialog, Instance);
            Find.WindowStack.Add(dialog);
        }
    }
}
