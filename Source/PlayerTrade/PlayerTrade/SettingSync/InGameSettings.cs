﻿using PlayerTrade.Net;
using PlayerTrade.SettingSync.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade.SettingSync
{
    public class InGameSettings : IExposable
    {
        public bool EnforceStoryteller;
        public StorytellerDef Storyteller; // If null, no storyteller settings will be applied
        public DifficultyDef Difficulty;
        public Difficulty CustomDifficulty;

        public void ExposeData()
        {
            Scribe_Values.Look(ref EnforceStoryteller, "enforceStoryteller");
            if (EnforceStoryteller)
            {
                Scribe_Defs.Look(ref Storyteller, "storyteller");
                Scribe_Defs.Look(ref Difficulty, "difficulty");
                if (Difficulty != null && Difficulty.isCustom)
                    Scribe_Deep.Look(ref CustomDifficulty, "customDifficulty");
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                    CustomDifficulty = new Difficulty(Difficulty);
            }
        }

        public void Apply()
        {
            // Apply storyteller settings
            if (Storyteller != null && Find.Storyteller.def != Storyteller)
            {
                Find.Storyteller.def = Storyteller;
                Find.Storyteller.Notify_DefChanged();
                
                Find.Storyteller.difficulty = Difficulty;
                Find.Storyteller.difficultyValues = CustomDifficulty;
            }
        }

        /// <summary>
        /// Push current local settings to server.
        /// </summary>
        public void Push()
        {
            PacketSyncSettings syncSettings = new PacketSyncSettings
            {
                Settings = new SerializedScribe<InGameSettings>(this)
            };
            RimLinkComp.Instance.Client.SendPacket(syncSettings);
        }
    }
}