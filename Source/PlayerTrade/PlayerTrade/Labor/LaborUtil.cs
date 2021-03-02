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

        public static void SendOffer(LaborOffer offer)
        {
            RimLinkComp.Instance.Client.Labor.Offers.Add(offer);
            RimLinkComp.Instance.Client.SendPacket(offer.ToPacket());
        }

        /// <summary>
        /// Try to find the labor offer associated with this pawn.<br />
        /// This first checks the <see cref="LentColonistComp"/>, if that fails it'll check the pawn GUID in all labor offers.
        /// </summary>
        public static LaborOffer FindLaborOffer(this Pawn pawn)
        {
            var lentColonistComp = pawn.TryGetComp<LentColonistComp>();
            if (lentColonistComp?.LaborOffer != null)
                return lentColonistComp.LaborOffer;

            var guid = pawn.TryGetComp<PawnGuidThingComp>().Guid;
            return RimLinkComp.Instance.ActiveLaborOffers.LastOrDefault(offer =>
            {
                foreach (Pawn offerPawn in offer.Colonists)
                {
                    if (offerPawn.TryGetComp<PawnGuidThingComp>().Guid == guid)
                        return true;
                }

                return false;
            });
        }
    }
}
