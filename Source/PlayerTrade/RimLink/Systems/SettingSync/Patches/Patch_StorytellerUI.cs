using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink.Systems.SettingSync.Patches
{
    /// <summary>
    /// Place an invisible button to block input in the storyteller selection window. A little bit jank but works well.
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUI), "DrawStorytellerSelectionInterface_NewTemp")]
    public static class Patch_StorytellerUI_DrawStorytellerSelectionInterface_NewTemp
    {
        private static void Prefix(Rect rect,
            ref StorytellerDef chosenStoryteller,
            ref DifficultyDef difficulty,
            ref Difficulty difficultyValues,
            Listing_Standard infoListing)
        {
            if (!RimLinkMod.Active || RimLink.Instance.InGameSettings == null || !RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return;
            
            if (Widgets.ButtonInvisible(rect.LeftPartPixels(rect.width - 16), false))
                Messages.Message("Rl_StorytellerSettingsLocked".Translate(), MessageTypeDefOf.RejectInput);
        }
    }
    
    /// <summary>
    /// Disable custom settings
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUI), "DrawCustomLeft")]
    public class Patch_StorytellerUI_DrawCustomLeft
    {
        private static void Prefix()
        {
            if (!RimLinkMod.Active || RimLink.Instance.InGameSettings == null || !RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return;
            
            GUI.enabled = false;
        }

        private static void Postfix()
        {
            if (!RimLinkMod.Active || RimLink.Instance.InGameSettings == null || !RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return;
            
            GUI.enabled = true;
        }
    }
    
    /// <summary>
    /// Disable custom settings
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUI), "DrawCustomRight")]
    public class Patch_StorytellerUI_DrawCustomRight
    {
        private static void Prefix()
        {
            if (!RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return;
            
            GUI.enabled = false;
        }

        private static void Postfix()
        {
            if (!RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return;
            
            GUI.enabled = true;
        }
    }
    
    /// <summary>
    /// Disable the select difficulty values button
    /// </summary>
    [HarmonyPatch(typeof(StorytellerUI), "MakeResetDifficultyFloatMenu")]
    public class Patch_StorytellerUI_MakeResetDifficultyFloatMenu
    {
        private static bool Prefix()
        {
            if (!RimLink.Instance.InGameSettings.EnforceStoryteller || Find.WindowStack.IsOpen<Dialog_SelectServerStoryteller>()) return true;

            var option = new FloatMenuOption("(locked)", () => { });
            option.Disabled = true;
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> { option }));

            return false;
        }
    }
}