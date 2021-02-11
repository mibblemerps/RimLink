using System;
using System.Threading.Tasks;
using PlayerTrade.Net;
using RimWorld;
using Verse;

namespace PlayerTrade.Trade
{
    public static class TradeUtil
    {
        public static float TotalWealth()
        {
            float totalWealth = 0f;
            foreach (Map map in Find.Maps)
                totalWealth += map.wealthWatcher.WealthTotal;
            return totalWealth;
        }

        public static void PresentTradeOffer(TradeOffer offer)
        {
            Letter letter = new ChoiceLetter_TradeOffer(offer);
            letter.ID = Find.UniqueIDsManager.GetNextLetterID();
            Find.LetterStack.ReceiveLetter(letter);
        }

        /// <summary>
        /// Initiate a trade with another player and open the trade window.
        /// </summary>
        /// <param name="negotiator">Negotiator for the trade (this doesn't affect prices)</param>
        /// <param name="player">Player to trade with</param>
        public static async Task InitiateTrade(Pawn negotiator, Player player)
        {
            // Send trade request packet
            RimLinkComp.Instance.Client.SendPacket(new PacketInitiateTrade
            {
                Guid = player.Guid
            });

            try
            {
                // Await response
                var packet = (PacketColonyResources) await RimLinkComp.Instance.Client.AwaitPacket(p =>
                    p is PacketColonyResources resourcePacket && resourcePacket.Guid == player.Guid);

                var playerTrader = new PlayerTrader(player, packet.Resources);
                Find.WindowStack.Add(new Dialog_PlayerTrade(negotiator, playerTrader));
            }
            catch (Exception e)
            {
                Log.Error("Error initiating trade", e);
            }
        }

        public static TradeOffer FormTradeOffer()
        {
            if (!(TradeSession.trader is PlayerTrader))
                throw new Exception("Attempt to send trade deal of a non-player trader");

            Log.Message("Forming trade offer...");
            var tradeOffer = new TradeOffer
            {
                For = ((PlayerTrader)TradeSession.trader).Player.Guid,
                From = RimLinkComp.Instance.Guid,
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
