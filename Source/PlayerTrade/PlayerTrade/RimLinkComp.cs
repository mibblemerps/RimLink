using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Raids;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class RimLinkComp : GameComponent, IDisposable
    {
        /// <summary>
        /// Uniquely identifies this player on the server(s) it plays on.
        /// </summary>
        public string Guid = System.Guid.NewGuid().ToString("N");
        /// <summary>
        /// A secret to prevent other people from impersonating our GUID.
        /// </summary>
        public string Secret;

        public List<TradeOffer> TradeOffersPendingFulfillment = new List<TradeOffer>();
        public List<BountyRaid> RaidsPending = new List<BountyRaid>();

        public Client Client;

        public RimLinkComp(Game game)
        {

        }

        public async void Init()
        {
            if (TradeOffersPendingFulfillment == null)
                TradeOffersPendingFulfillment = new List<TradeOffer>();
            if (RaidsPending == null)
                RaidsPending = new List<BountyRaid>();

            string ip = PlayerTradeMod.Instance.Settings.ServerIp;
            if (string.IsNullOrWhiteSpace(ip))
            {
                Log.Message("Not connecting to trade server: No IP set");
                return;
            }

            if (string.IsNullOrWhiteSpace(Secret))
            {
                // Generate a secret (not crytographically great - but it'll do for this)
                Secret = BitConverter.ToString(System.Guid.NewGuid().ToByteArray()).Replace("-", "");
            }

            if (!PlayerTradeMod.Instance.Connected)
            {
                // Connect
                Log.Message("Connecting to: " + ip);
                Client = new Client(this);
                await PlayerTradeMod.Instance.Connect();
            }

            Log.Message("Player trade active. GUID: " + Guid);

            // Now tradable
            Client.IsTradableNow = true;

        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Current.Game.tickManager.TicksGame % 1200 == 0)
            {
                // Mark dirty (send update packet)
                Client.MarkDirty();
            }

            var toRemove = new List<BountyRaid>();
            foreach (BountyRaid raid in RaidsPending)
            {
                if (--raid.ArrivesInTicks <= 0)
                {
                    // Trigger raid
                    raid.Execute();
                    toRemove.Add(raid);
                }
            }
            foreach (BountyRaid raid in toRemove)
                RaidsPending.Remove(raid);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Secret, "secret");
            Scribe_Collections.Look(ref TradeOffersPendingFulfillment, "trade_offers_pending_fulfillment");
            Scribe_Collections.Look(ref RaidsPending, "raids_pending");
        }

        public void Dispose()
        {
            Log.Message("TODO: disconnect on game comp dispose (if that's a thing that works)");
        }

        /// <summary>
        /// Find (get or create) the RimLink game component.
        /// </summary>
        /// <returns></returns>
        public static RimLinkComp Find()
        {
            RimLinkComp comp = Current.Game.GetComponent<RimLinkComp>();
            if (comp == null)
            {
                comp = new RimLinkComp(Current.Game);
                Current.Game.components.Add(comp);
            }
            return comp;
        }
    }
}
