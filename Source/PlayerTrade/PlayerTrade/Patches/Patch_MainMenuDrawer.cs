using HarmonyLib;
using RimWorld;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(MainMenuDrawer), "Init")]
    public class Patch_MainMenuDrawer_Init
    {
        private static void Postfix()
        {
            MainMenuWidget.Init();
        }
    }

    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    public class Patch_MainMenuDrawer_MainMenuOnGUI
    {
        private static void Postfix()
        {
            MainMenuWidget.OnGUI();
        }
    }
}
