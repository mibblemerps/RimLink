using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.SettingSync
{
    public class Dialog_SelectServerStoryteller : Page_SelectStorytellerInGame
    {
        protected InGameSettings Settings => RimLinkComp.Instance.InGameSettings;
        
        private Listing_Standard _selectedStorytellerInfoListing = new Listing_Standard();

        public Dialog_SelectServerStoryteller()
        {
            doCloseButton = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            if (Settings.Storyteller == null)
            {
                Settings.Storyteller = Find.Storyteller.def;
                Settings.Difficulty = Find.Storyteller.difficulty;
                Settings.CustomDifficulty = Find.Storyteller.difficultyValues;
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            Rect mainRect = GetMainRect(rect);

            StorytellerUI.DrawStorytellerSelectionInterface_NewTemp(mainRect,
              ref Settings.Storyteller, ref Settings.Difficulty, ref Settings.CustomDifficulty, _selectedStorytellerInfoListing);
            
            Rect buttonsRect = rect.BottomPartPixels(38);
            if (Widgets.ButtonText(buttonsRect.RightPart(0.4f), "Apply on Server"))
            {
                // Push changes to server
                Settings.EnforceStoryteller = true;
                Settings.Push();
                Close();
            }

            if (Widgets.ButtonText(buttonsRect.LeftPart(0.4f), "Remove Server Storyteller"))
            {
                // Clear storyteller settings then push
                Settings.EnforceStoryteller = false;
                Settings.Storyteller = null;
                Settings.Difficulty = null;
                Settings.CustomDifficulty = null;
                Settings.Push();
                Close();
            }
        }
    }
}