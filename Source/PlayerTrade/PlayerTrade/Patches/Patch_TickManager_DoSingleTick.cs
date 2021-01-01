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
                if (RimLinkComp.Find().Client == null)
                    return;

                // Fulfill pending trades
                foreach (TradeOffer offer in RimLinkComp.Find().Client.OffersToFulfillNextTick)
                    offer.Fulfill(offer.IsForUs);
                RimLinkComp.Find().Client.OffersToFulfillNextTick.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Exception ticking RimLink!", e);
            }
        }
    }
}
