using HarmonyLib;
using RimWorld;

namespace RimLink.Patches
{
    /// <summary>
    /// Initializes the main menu widget.
    /// </summary>
    [HarmonyPatch(typeof(MainMenuDrawer), "Init")]
    public class Patch_MainMenuDrawer_Init
    {
        private static void Postfix()
        {
            MainMenuWidget.Init();
        }
    }

    /// <summary>
    /// Draws the main menu widget.
    /// </summary>
    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    public class Patch_MainMenuDrawer_MainMenuOnGUI
    {
        private static void Postfix()
        {
            MainMenuWidget.OnGUI();
        }
    }
}
