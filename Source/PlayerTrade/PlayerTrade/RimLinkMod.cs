using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using PlayerTrade.Net;
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

        public static bool Active
        {
            get
            {
                if (RimLinkComp.Instance == null || RimLinkComp.Instance.Client == null)
                    return false;

                try
                {
                    return RimLinkComp.Instance.Client.State == Client.ConnectionState.Authenticated;
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
