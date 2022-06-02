using RimLink.Net;
using RimLink.Systems.SettingSync.Packets;
using RimWorld;
using Verse;

namespace RimLink.Systems.SettingSync
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
            ApplyStorytellerSettings();
        }

        public void ApplyStorytellerSettings()
        {
            if (EnforceStoryteller)
            {
                if (Find.Storyteller.def != Storyteller)
                {
                    Find.Storyteller.def = Storyteller;
                    Find.Storyteller.Notify_DefChanged();
                }

                Find.Storyteller.difficultyDef = Difficulty;
                Find.Storyteller.difficulty = CustomDifficulty;
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
            RimLink.Instance.Client.SendPacket(syncSettings);
        }
    }
}