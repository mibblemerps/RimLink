using System;
using System.Collections.Generic;
using PlayerTrade.Net;
using Verse;

namespace PlayerTrade
{
    public class RimLinkGameComponent : GameComponent, IDisposable
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

        public RimLinkGameComponent(Game game)
        {

        }

        public async void Init()
        {
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
                await PlayerTradeMod.Instance.Connect();
            }

            Log.Message("Player trade active. GUID: " + Guid);

            // Now tradable
            PlayerTradeMod.Instance.Client.IsTradableNow = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Secret, "secret");
            Scribe_Collections.Look(ref TradeOffersPendingFulfillment, "trade_offers_pending_fulfillment");
        }

        public void Dispose()
        {
            Log.Message("TODO: disconnect on game comp dispose (if that's a thing that works)");
        }

        /// <summary>
        /// Find (get or create) the RimLink game component.
        /// </summary>
        /// <returns></returns>
        public static RimLinkGameComponent Find()
        {
            RimLinkGameComponent comp = Current.Game.GetComponent<RimLinkGameComponent>();
            if (comp == null)
            {
                comp = new RimLinkGameComponent(Current.Game);
                Current.Game.components.Add(comp);
            }
            return comp;
        }
    }
}
