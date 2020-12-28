using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using StringBuilder = System.Text.StringBuilder;

namespace PlayerTrade
{
    public static class TradeOfferUtil
    {
        public static void PresentTradeOffer(TradeOffer offer)
        {
            Find.LetterStack.ReceiveLetter(new ChoiceLetter_TradeOffer(offer));
        }

        public static void TradeOfferSuccess(TradeOffer offer)
        {
            
        }

        public static TradeOffer FormTradeOffer()
        {
            if (!(TradeSession.trader is PlayerTrader))
                throw new Exception("Attempt to send trade deal of a non-player trader");

            Log.Message("Forming trade offer...");
            var tradeOffer = new TradeOffer
            {
                For = ((PlayerTrader)TradeSession.trader).Username,
                From = PlayerTradeMod.Instance.Client.Username,
                Fresh = true,
                Guid = Guid.NewGuid()
            };
            foreach (Tradeable tradeable in TradeSession.deal.AllTradeables)
            {
                if (tradeable.ActionToDo == TradeAction.PlayerBuys)
                {
                    tradeOffer.Things.Add(new TradeOffer.TradeThing(tradeable.thingsColony, tradeable.thingsTrader,
                        -tradeable.CountToTransfer));
                }
                else if (tradeable.ActionToDo == TradeAction.PlayerSells)
                {
                    tradeOffer.Things.Add(new TradeOffer.TradeThing(tradeable.thingsColony, tradeable.thingsTrader,
                        -tradeable.CountToTransfer));
                }
            }

            return tradeOffer;
        }
    }
}
