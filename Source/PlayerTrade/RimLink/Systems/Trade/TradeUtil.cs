using System;
using System.Threading.Tasks;
using RimLink.Util;
using RimLink.Core;
using RimLink.Net.Packets;
using RimLink.Systems.Trade.Packets;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimLink.Systems.Trade
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
            var loadingTradeWindow = new Dialog_LoadingTradeWindow();
            Find.WindowStack.Add(loadingTradeWindow);
            
            // Send trade request packet
            RimLink.Instance.Client.SendPacket(new PacketInitiateTrade
            {
                Guid = player.Guid
            });

            try
            {
                // Await response
                var packet = (PacketColonyResources) await RimLink.Instance.Client.AwaitPacket(p =>
                    p is PacketColonyResources resourcePacket && resourcePacket.Guid == player.Guid);

                var playerTrader = new PlayerTrader(player, packet.Resources);
                Find.WindowStack.Add(new Dialog_PlayerTrade(negotiator, playerTrader));
            }
            catch (Exception e)
            {
                Log.Error("Error initiating trade", e);
            }
            
            loadingTradeWindow.Close();
        }

        public static TradeOffer FormTradeOffer()
        {
            if (!(TradeSession.trader is PlayerTrader))
                throw new Exception("Attempt to send trade deal of a non-player trader");

            Log.Message("Forming trade offer...");
            var tradeOffer = new TradeOffer
            {
                For = ((PlayerTrader)TradeSession.trader).Player.Guid,
                From = RimLink.Instance.Guid,
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

        /// <summary>
        /// <p>Retract a trade offer.</p>
        /// <p>If this is called by the <b>sender</b>, a packet is sent to inform the other player the trade is retracted.</p>
        /// <p>If this is called by the <b>receiver</b>, a letter is presented indicating the offer is retracted.</p>
        /// <p>In either case, the trade offer is removed from the active trade offers, so it will no longer be able to be fulfilled.</p>
        /// </summary>
        /// <param name="offer">Offer to retract.</param>
        public static void RetractOffer(TradeOffer offer)
        {
            RimLink.Instance.Get<TradeSystem>().ActiveTradeOffers.Remove(offer);
            
            if (!offer.IsForUs)
            {
                // Is our offer, send retraction to the target player
                RimLink.Instance.Client.SendPacket(new PacketRetractTrade {Guid = offer.Guid, For = offer.For});
            }
            else
            {
                // We received this offer. Tell player the offer has been retracted and remove it locally.
                RimLink.Instance.Get<TradeSystem>().ActiveTradeOffers.Remove(offer);

                Find.LetterStack.ReceiveLetter("Rl_TradeRetracted".Translate(offer.From.GuidToName()),
                    "Rl_TradeRetractedDesc".Translate(offer.From.GuidToName(true)),
                    LetterDefOf.NegativeEvent);
            }
        }
    }
}
