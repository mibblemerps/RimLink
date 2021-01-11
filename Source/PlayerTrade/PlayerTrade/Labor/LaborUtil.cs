using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PlayerTrade.Labor
{
    public static class LaborUtil
    {
        public static void PresentLendColonistOffer(LaborOffer offer)
        {
            Letter letter = new ChoiceLetter_LaborOffer(offer);
            letter.ID = Find.UniqueIDsManager.GetNextLetterID();
            Find.LetterStack.ReceiveLetter(letter);
        }

        public static async Task SendOffer(LaborOffer offer)
        {
            RimLinkComp.Find().Client.Labor.Offers.Add(offer);
            await RimLinkComp.Find().Client.SendPacket(offer.ToPacket());
        }
    }
}
