using HarmonyLib;
using PlayerTrade.Trade;
using RimWorld;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(Tradeable), "Interactive", MethodType.Getter)]
    public static class Patch_Tradeable_Interactive
    {
        private static void Postfix(ref bool __result)
        {
            if (TradeSession.trader is PlayerTrader)
            {
                __result = true; // force
            }
        }
    }
}
