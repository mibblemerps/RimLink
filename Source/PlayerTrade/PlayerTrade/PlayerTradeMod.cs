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
    public class PlayerTradeMod : Mod
    {
        public static PlayerTradeMod Instance { get; private set; }

        public ModSettings Settings;

        public static bool Connected
        {
            get
            {
                try
                {
                    return RimLinkComp.Instance.Client.Tcp.Connected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public PlayerTradeMod(ModContentPack content) : base(content)
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
            listing.Begin(inRect);

            Settings.Username = listing.TextEntryLabeled("Username ", Settings.Username);

            listing.Label(""); // Gap

            listing.Label("Trade Server IP");
            Settings.ServerIp = listing.TextEntry(Settings.ServerIp);

            listing.CheckboxLabeled("Logging Enabled", ref Settings.LoggingEnabled);
            Log.Enabled = Settings.LoggingEnabled;

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Player Trade";
        }
    }
}
