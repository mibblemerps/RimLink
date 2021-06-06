using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade.Patches
{
    /// <summary>
    /// Prevents the ability to change storyteller settings when the storyteller is set by the server.
    /// </summary>
    [HarmonyPatch(typeof(Page_SelectStorytellerInGame), "DoWindowContents")]
    public static class Patch_Page_SelectStorytellerInGame
    {
        private static void Postfix(Page_SelectStorytellerInGame __instance, Rect rect)
        {
            if (!RimLinkComp.Instance.InGameSettings.EnforceStoryteller)
                return; // Skip patch

            Rect labelRect = rect.TopPartPixels(35).RightHalf();
            labelRect.x -= 10;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(labelRect, "Rl_StorytellerSettingsLocked".Translate().ToString().Colorize(ColoredText.RedReadable));
            Text.Anchor = TextAnchor.UpperLeft;

            // Reapply settings
            RimLinkComp.Instance.InGameSettings.ApplyStorytellerSettings();
        }
    }
}