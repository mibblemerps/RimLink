using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrade.Labor;
using PlayerTrade.Net;
using PlayerTrade.Raids;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class RimLinkComp : GameComponent
    {
        /// <summary>
        /// The last known instance of the RimLink comp. This should never be used for normally accessing RimLink (use <see cref="Find"/> instead).<br />
        /// This is used for the shutdown procedure.
        /// </summary>
        public static RimLinkComp LastInstance;

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

        public List<LaborOffer> ActiveLaborOffers = new List<LaborOffer>();

        /// <summary>
        /// Pawns that have been sent and should be removed.
        /// </summary>
        public List<Pawn> PawnsToRemove = new List<Pawn>();

        public Client Client;

        public RimLinkComp(Game game)
        {

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            Log.Message("RimLink comp init");

            Init();
        }

        public async void Init()
        {
            if (TradeOffersPendingFulfillment == null)
                TradeOffersPendingFulfillment = new List<TradeOffer>();
            if (RaidsPending == null)
                RaidsPending = new List<BountyRaid>();
            if (ActiveLaborOffers == null)
                ActiveLaborOffers = new List<LaborOffer>();

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

            // Connect
            Log.Message("Connecting to: " + ip);
            Client = new Client(this);
            await Client.Connect(PlayerTradeMod.Instance.Settings.ServerIp);

            Log.Message("Player trade active. GUID: " + Guid);

            // Now tradable
            Client.IsTradableNow = true;

        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Current.Game.tickManager.TicksGame % 300 == 0 && PlayerTradeMod.Connected)
            {
                // Mark dirty (send update packet)
                Client?.MarkDirty();
            }

            var raidsToRemove = new List<BountyRaid>();
            foreach (BountyRaid raid in RaidsPending)
            {
                if (--raid.ArrivesInTicks <= 0)
                {
                    // Trigger raid
                    raid.Execute();
                    raidsToRemove.Add(raid);
                }
            }
            foreach (BountyRaid raid in raidsToRemove)
                RaidsPending.Remove(raid);

            // Remove sent pawns
            foreach (Pawn pawn in PawnsToRemove)
                pawn.Destroy(DestroyMode.Vanish);
            PawnsToRemove.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Secret, "secret");
            Scribe_Collections.Look(ref TradeOffersPendingFulfillment, "trade_offers_pending_fulfillment");
            Scribe_Collections.Look(ref RaidsPending, "raids_pending");
            Scribe_Collections.Look(ref ActiveLaborOffers, "active_labor_offers");
            Scribe_Collections.Look(ref PawnsToRemove, "pawns_to_remove", LookMode.Reference);
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
            LastInstance = comp;
            return comp;
        }
    }
}
