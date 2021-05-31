using HarmonyLib;
using PlayerTrade.Trade;
using RimWorld;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(TradeUtility), "EverPlayerSellable")]
    public class Patch_TradeUtility_EverPlayerSellable
    {
        public static bool ForceEnable = false;

        private static bool Prefix(ThingDef def, ref bool __result)
        {
            if (ForceEnable || TradeSession.trader is PlayerTrader)
            {
                // Custom CanSell logic - skip the can sell check
                __result = (def.category == ThingCategory.Item || def.category == ThingCategory.Pawn || def.category == ThingCategory.Building)
                           && (def.category != ThingCategory.Building || def.Minifiable)
                           && (def.tradeability != Tradeability.None); // has to be at least tradeable in some form
                return false;
            }

            // Not player trader - default behaviour
            return true;
        }
    }
}
