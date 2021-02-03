using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrade.Anticheat;
using PlayerTrade.Labor;
using PlayerTrade.Mail;
using PlayerTrade.Net;
using PlayerTrade.Raids;
using PlayerTrade.Trade;
using RimWorld;
using UnityEngine;
using Verse;

namespace PlayerTrade
{
    public class RimLinkComp : GameComponent
    {
        private const int MaxReconnectTime = 30;

        /// <summary>
        /// The last known instance of the RimLink comp. This should never be used for normally accessing RimLink (use <see cref="Find"/> instead).<br />
        /// This is used for the shutdown procedure.
        /// </summary>
        public static RimLinkComp Instance;

        /// <summary>
        /// Uniquely identifies this player on the server(s) it plays on.
        /// </summary>
        public string Guid = System.Guid.NewGuid().ToString("N");
        /// <summary>
        /// A secret to prevent other people from impersonating our GUID.
        /// </summary>
        public string Secret;

        /// <summary>
        /// Is anticheat applied to this save?
        /// </summary>
        public bool Anticheat;

        public List<Player> RememberedPlayers;

        public List<TradeOffer> TradeOffersPendingFulfillment = new List<TradeOffer>();
        public List<BountyRaid> RaidsPending = new List<BountyRaid>();
        public List<LaborOffer> ActiveLaborOffers = new List<LaborOffer>();

        /// <summary>
        /// Pawns that have been sent and should be removed.
        /// </summary>
        public List<Pawn> PawnsToRemove = new List<Pawn>();

        public Client Client;

        public float TimeUntilReconnect => Mathf.Max(0, _reconnectIn);
        public bool Connecting => _connecting;

        private bool _connecting;
        private float _reconnectIn = float.NaN;
        private int _failedAttempts;

        public RimLinkComp(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            Instance = this;

            Log.Message("RimLink comp init");

            Init();
            RemoveExpiredOfferLetters();
        }

        public async Task Connect()
        {
            if (_connecting)
            {
                Log.Warn("Attempt to connect while we're already trying to connect!");
                return;
            }

            Log.Message("Connecting to: " + PlayerTradeMod.Instance.Settings.ServerIp);
            _connecting = true;
            Client = new Client(this);
            Client.Connected += OnClientConnected;
            Client.Disconnected += ClientOnDisconnected;
            Client.PlayerConnected += OnPlayerConnected;
            try
            {
                await Client.Connect(PlayerTradeMod.Instance.Settings.ServerIp);
                // _connecting is set back to false in OnClientConnected
            }
            catch (ConnectionFailedException e)
            {
                _connecting = false;
                throw;
            }
        }

        public async void Init()
        {
            // Initialize lists
            if (RememberedPlayers == null)
                RememberedPlayers = new List<Player>();
            if (TradeOffersPendingFulfillment == null)
                TradeOffersPendingFulfillment = new List<TradeOffer>();
            if (RaidsPending == null)
                RaidsPending = new List<BountyRaid>();
            if (ActiveLaborOffers == null)
                ActiveLaborOffers = new List<LaborOffer>();

            // Generate a secret if we don't have one (not crytographically great - but it'll do for this)
            if (string.IsNullOrWhiteSpace(Secret))
                Secret = BitConverter.ToString(System.Guid.NewGuid().ToByteArray()).Replace("-", "");

            // Apply anticheat
            if (Anticheat)
                AnticheatUtil.ApplyAnticheat();

            // Get IP
            string ip = PlayerTradeMod.Instance.Settings.ServerIp;
            if (string.IsNullOrWhiteSpace(ip))
            {
                Log.Message("Not connecting to trade server: No IP set");
                return;
            }

            // Connect
            QueueConnect();
        }

        public void QueueConnect(float seconds = 0f)
        {
            _reconnectIn = seconds;
        }

        public void OnClientConnected(object sender, EventArgs args)
        {
            _connecting = false;
            _failedAttempts = 0; // reset failed attempts

            Log.Message("Connected to server. GUID: " + Guid);
            Messages.Message($"Connected to server", MessageTypeDefOf.NeutralEvent, false);

            if (!Anticheat && Client.GameSettings.Anticheat)
                AnticheatUtil.ShowEnableAnticheatDialog();

            Client.MarkDirty();
        }

        private void ClientOnDisconnected(object sender, EventArgs e)
        {
            Messages.Message("Disconnected from server", MessageTypeDefOf.NeutralEvent, false);
            QueueConnect();
        }

        private void ReconnectUpdate()
        {
            if (!float.IsNaN(_reconnectIn) && !_connecting)
            {
                _reconnectIn -= Time.deltaTime;

                if (_reconnectIn <= 0f)
                {
                    // Reconnect now
                    _reconnectIn = float.NaN;

                    _ = Connect().ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception?.InnerException != null &&
                            t.Exception.InnerException is ConnectionFailedException connectionException)
                        {
                            if (!connectionException.AllowReconnect)
                            {
                                // Cannot auto reconnect. Abort reconnecting and show connection failed dialog.
                                ShowConnectionFailedDialog(connectionException);
                                _reconnectIn = float.NaN;
                                return;
                            }
                        }
                        
                        if (t.IsFaulted)
                        {
                            // Queue next attempt. Reconnect time doubles each failed attempt, up to a defined maximum
                            _reconnectIn = Mathf.Min(Mathf.Pow(2, ++_failedAttempts), MaxReconnectTime);
                            Log.Message($"Reconnect attempt in {_reconnectIn} seconds ({_failedAttempts} failed attempts)");
                        }
                    });
                }
            }
        }

        public void ShowConnectionFailedDialog(ConnectionFailedException exception)
        {
            var connectionFailedMsgBox = new Dialog_MessageBox(exception.Message, title: "Server Connection Failed",
                buttonAText: "Quit to Main Menu", buttonAAction: GenScene.GoToMainMenu,
                buttonBText: "Close");
            connectionFailedMsgBox.forcePause = true;
            Verse.Find.WindowStack.Add(connectionFailedMsgBox);
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();

            Client?.Update();

            ReconnectUpdate();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Current.Game.tickManager.TicksGame % 300 == 0 && PlayerTradeMod.Connected)
            {
                // Mark dirty (send update packet)
                Client?.MarkDirty();
            }

            // Fulfill pending trades
            foreach (TradeOffer offer in TradeOffersPendingFulfillment)
                offer.Fulfill(offer.IsForUs);
            TradeOffersPendingFulfillment.Clear();

            // Trigger pending raids
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

        private void OnPlayerConnected(object sender, Player e)
        {
            // Remove and re-add player to remembered players list
            RememberedPlayers.RemoveAll(player => player.Guid == e.Guid);
            RememberedPlayers.Add(e);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Secret, "secret");
            Scribe_Collections.Look(ref RememberedPlayers, "players", LookMode.Deep);
            Scribe_Collections.Look(ref TradeOffersPendingFulfillment, "trade_offers_pending_fulfillment");
            Scribe_Collections.Look(ref RaidsPending, "raids_pending");
            Scribe_Collections.Look(ref ActiveLaborOffers, "active_labor_offers");
            Scribe_Collections.Look(ref PawnsToRemove, "pawns_to_remove", LookMode.Reference);
            Scribe_Values.Look(ref Anticheat, "anticheat", false, true);
        }

        /// <summary>
        /// Find (get or create) the RimLink game component.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use Instance instead")]
        public static RimLinkComp Find()
        {
            return Instance;
        }

        /// <summary>
        /// Remove any trade/labor offer letters that are expired and are no longer relevant.<br />
        /// Most of the info in these letters is still readable in the letter history, however the cannot be accepted.
        /// </summary>
        public static void RemoveExpiredOfferLetters()
        {
            foreach (Letter letter in Verse.Find.LetterStack.LettersListForReading)
            {
                if (letter is ChoiceLetter_TradeOffer tradeOfferLetter && tradeOfferLetter.Offer == null)
                    Verse.Find.LetterStack.RemoveLetter(letter);

                if (letter is ChoiceLetter_LaborOffer laborOfferLetter && laborOfferLetter.LaborOffer == null)
                    Verse.Find.LetterStack.RemoveLetter(letter);
            }
        }
    }
}
