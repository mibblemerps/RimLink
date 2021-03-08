using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrade.Anticheat;
using PlayerTrade.Mail;
using PlayerTrade.Missions;
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
        public const float UpdateInterval = 2f;

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

        public List<BountyRaid> RaidsPending = new List<BountyRaid>();
        public List<MissionOffer> Missions = new List<MissionOffer>();
        public List<Pawn> EscapingLentColonists = new List<Pawn>();

        /// <summary>
        /// Player factions. GUID -> Faction
        /// </summary>
        public Dictionary<string, Faction> PlayerFactions = new Dictionary<string, Faction>();

        public float QueuedResearch;

        private List<string> _tmpPlayerFactionGuids;
        private List<Faction> _tmpPlayerFactions;

        public Client Client;

        /// <summary>
        /// Should we attempt to reconnect when we see a disconnection? When we've been kicked, this is set to false.
        /// </summary>
        public bool ReconnectOnNextDisconnect = true;
        public float TimeUntilReconnect => Mathf.Max(0, _reconnectIn);
        public bool Connecting => _connecting;

        private float _lastUpdateSent = 0f;

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

            _connecting = true;
            Client = new Client(this);
            Client.Connected += OnClientConnected;
            Client.PlayerConnected += OnPlayerConnected;
            Client.PlayerUpdated += OnPlayerUpdated;
            try
            {
                await Client.Connect(RimLinkMod.Instance.Settings.ServerIp, RimLinkMod.Instance.Settings.ServerPort);
                // _connecting is set back to false in OnClientConnected
            }
            catch (ConnectionFailedException e)
            {
                _connecting = false;
                throw;
            }
        }

        public void Init()
        {
            // Initialize lists
            if (RememberedPlayers == null)
                RememberedPlayers = new List<Player>();
            if (RaidsPending == null)
                RaidsPending = new List<BountyRaid>();
            if (Missions == null)
                Missions = new List<MissionOffer>();
            if (PlayerFactions == null)
                PlayerFactions = new Dictionary<string, Faction>();
            if (EscapingLentColonists == null)
                EscapingLentColonists = new List<Pawn>();

            // Generate a secret if we don't have one (not crytographically great - but it'll do for this)
            if (string.IsNullOrWhiteSpace(Secret))
                Secret = BitConverter.ToString(System.Guid.NewGuid().ToByteArray()).Replace("-", "");

            // Apply anticheat
            if (Anticheat)
                AnticheatUtil.ApplyAnticheat();

            // Get IP
            string ip = RimLinkMod.Instance.Settings.ServerIp;
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
            if (Client?.Tcp != null && Client.Tcp.Connected)
            {
                Log.Warn("Tried to queue connect while we're already connected.");
                return;
            }

            _reconnectIn = seconds;
        }

        public void OnClientConnected(object sender, EventArgs args)
        {
            _connecting = false;
            _failedAttempts = 0; // reset failed attempts
            ReconnectOnNextDisconnect = true; // enable auto reconnect

            Client.Disconnected += ClientOnDisconnected;

            Log.Message("Connected to server. GUID: " + Guid);
            Messages.Message($"Connected to server", MessageTypeDefOf.NeutralEvent, false);

            if (!Anticheat && Client.GameSettings.Anticheat)
                AnticheatUtil.ShowEnableAnticheatDialog();

            Client.MarkDirty();
        }

        private void ClientOnDisconnected(object sender, EventArgs e)
        {
            Messages.Message("Disconnected from server", MessageTypeDefOf.NeutralEvent, false);
            if (ReconnectOnNextDisconnect)
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
                                _reconnectIn = float.NaN;
                                ShowConnectionFailedDialog(connectionException);
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

            // Send update every x seconds
            if (Time.realtimeSinceStartup - _lastUpdateSent >= UpdateInterval && RimLinkMod.Active)
            {
                _lastUpdateSent = Time.realtimeSinceStartup;
                Client?.MarkDirty(); // marking as dirty causes a new update to be sent
            }

            foreach (Pawn escapingLentColonist in EscapingLentColonists)
            {
                var comp = escapingLentColonist.TryGetComp<LentColonistComp>();
                comp.TryEscape();
            }

        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

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
        }

        private void OnPlayerConnected(object sender, Player e)
        {
            // Remove and re-add player to remembered players list
            RememberedPlayers.RemoveAll(player => player.Guid == e.Guid);
            RememberedPlayers.Add(e);

            if (!PlayerFactions.ContainsKey(e.Guid))
            {
                // Add faction
                Faction playerFaction = FactionGenerator.NewGeneratedFaction(DefDatabase<FactionDef>.GetNamed("OtherPlayer"));
                PlayerFactions.Add(e.Guid, playerFaction);
                Verse.Find.FactionManager.Add(playerFaction);

                Log.Message($"Generated faction for player {e.Name} ({e.Guid}).");
            }
        }

        private void OnPlayerUpdated(object sender, Client.PlayerUpdateEventArgs e)
        {
            // Update player faction name
            PlayerFactions[e.Player.Guid].Name = e.Player.Name;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Guid, "guid");
            Scribe_Values.Look(ref Secret, "secret");
            Scribe_Collections.Look(ref RememberedPlayers, "players", LookMode.Deep);
            Scribe_Collections.Look(ref RaidsPending, "raids_pending");
            Scribe_Collections.Look(ref Missions, "missions", LookMode.Deep);
            Scribe_Collections.Look(ref EscapingLentColonists, "escaping_lent_colonists", LookMode.Reference);
            Scribe_Collections.Look(ref PlayerFactions, "player_factions", LookMode.Value, LookMode.Reference, ref _tmpPlayerFactionGuids, ref _tmpPlayerFactions);
            Scribe_Values.Look(ref Anticheat, "anticheat", false, true);
            Scribe_Values.Look(ref QueuedResearch, "queued_research");
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

                if (letter is ChoiceLetter_LaborOffer laborOfferLetter && laborOfferLetter.MissionOffer == null)
                    Verse.Find.LetterStack.RemoveLetter(letter);
            }
        }
    }
}
