using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace PlayerTrade.Patches
{
    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class Patch_TickManager_DoSingleTick
    {
        private static void Postfix()
        {
            try
            {
                if (PlayerTradeMod.Instance.Client == null)
                    return;

                // Fulfill pending trades
                foreach (TradeOffer offer in PlayerTradeMod.Instance.Client.OffersToFulfillNextTick)
                    offer.Fulfill(offer.IsForUs);
                PlayerTradeMod.Instance.Client.OffersToFulfillNextTick.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Exception ticking RimLink!", e);
            }
        }
    }
}
