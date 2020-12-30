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
    public static class TradeUtil
    {
        public static void PresentTradeOffer(TradeOffer offer)
        {
            Find.LetterStack.ReceiveLetter(new ChoiceLetter_TradeOffer(offer));
        }

        /// <summary>
        /// Initiate a trade with another player and open the trade window.
        /// </summary>
        /// <param name="negotiator">Negotiator for the trade (this doesn't affect prices)</param>
        /// <param name="username">Username of player to trade with</param>
        public static async Task InitiateTrade(Pawn negotiator, string username)
        {
            PacketColonyResources packet = await PlayerTradeMod.Instance.Client.GetColonyResources(username);

            var playerTrader = new PlayerTrader(username, packet.Resources);
            Find.WindowStack.Add(new Dialog_PlayerTrade(negotiator, playerTrader));
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
