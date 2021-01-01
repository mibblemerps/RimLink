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

        public bool Connected
        {
            get
            {
                try
                {
                    return RimLinkComp.Find().Client.Tcp.Connected;
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

            listing.Label("Connect to Server");
            Settings.ServerIp = listing.TextEntry(Settings.ServerIp);
            if (listing.ButtonText("Connect"))
            {
                Debug.Log("Connect...");
                _ = Connect();
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Player Trade";
        }

        public async Task Connect()
        {
            Client client = RimLinkComp.Find().Client;

            try
            {
                Log.Message($"Connecting to {Settings.ServerIp}...");
                await client.Connect(Settings.ServerIp);
                Log.Message("Connected");

                Find.WindowStack.TryRemove(typeof(Dialog_ModSettings)); // Close mod settings dialog
                Find.WindowStack.Add(new Dialog_MessageBox($"Connected to trade server ({Settings.ServerIp}).\n\nUse the Comms Console to initiate player trades.", "Ok", title: "Player Trade"));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to connect to trade server.", e);
            }
        }

        public bool IsTradableNow(Map map)
        {
            return CommsConsoleUtility.PlayerHasPoweredCommsConsole(map);
        }
    }
}
