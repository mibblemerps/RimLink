using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimLink.Anticheat;
using RimLink.Core;
using RimLink.Net;
using RimLink.Systems;
using RimLink.Systems.Chat;
using RimLink.Systems.Mail;
using RimLink.Systems.Mechanoids;
using RimLink.Systems.Missions;
using RimLink.Systems.Raids;
using RimLink.Systems.SettingSync;
using RimLink.Systems.Trade;
using RimLink.Systems.World;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimLink
{
    public class RimLink : GameComponent
    {
        public const float UpdateInterval = 2f;

        public static RimLink Instance;

        /// <summary>Uniquely identifies this player on the server(s) it plays on.</summary>
        public string Guid = System.Guid.NewGuid().ToString("N");
        
        /// <summary>A secret to prevent other people from impersonating our GUID.</summary>
        public string Secret;

        /// <summary>Is anticheat applied to this save?</summary>
        public bool Anticheat;

        /// <summary>Are we an admin?</summary>
        public bool IsAdmin;

        public InGameSettings InGameSettings => Get<SettingSyncSystem>().Settings;

        public List<Player> RememberedPlayers;

        /// <summary>
        /// Player factions. GUID -> Faction
        /// </summary>
        public Dictionary<string, Faction> PlayerFactions = new Dictionary<string, Faction>();

        private List<string> _tmpPlayerFactionGuids; // for Scribe
        private List<Faction> _tmpPlayerFactions; // for Scribe

        /// <summary>Current client instance. When reconnecting a new client is created.</summary>
        public Client Client;
        /// <summary>Handles connecting, reconnecting and disconnects.</summary>
        public readonly ClientConnectionManager ConnectionManager;

        public readonly Dictionary<Type, ISystem> Systems = new Dictionary<Type, ISystem>();

        private float _lastUpdateSent = 0f;

        public RimLink(Game game)
        {
            RimLinkMod.Init(); // this will init the main mod if needed

            ConnectionManager = new ClientConnectionManager(this);
            
            AddSystem(new SettingSyncSystem());
            AddSystem(new TradeSystem());
            AddSystem(new RaidSystem());
            AddSystem(new MissionSystem());
            AddSystem(new MailSystem());
            AddSystem(new ChatSystem());
            AddSystem(new MechanoidSystem());
            AddSystem(new WorldSystem());
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            Instance = this;

            Log.Message("RimLink GameComponent FinalizeInit");

            Init();
            RemoveExpiredOfferLetters();
        }
        
        public void Init()
        {
            // Initialize lists
            if (RememberedPlayers == null)
                RememberedPlayers = new List<Player>();
            if (PlayerFactions == null)
                PlayerFactions = new Dictionary<string, Faction>();

            // Generate a secret if we don't have one (not crytographically great - but it'll do for this)
            if (string.IsNullOrWhiteSpace(Secret))
                Secret = BitConverter.ToString(System.Guid.NewGuid().ToByteArray()).Replace("-", "");

            // Apply anticheat
            if (Anticheat)
                AnticheatUtil.ApplyAnticheat();

            // Connect
            ConnectionManager.QueueConnect();
        }

        /// <summary>
        /// Get the instance of a mod sub-system.
        /// </summary>
        /// <typeparam name="T">Which system</typeparam>
        public T Get<T>() where T : ISystem
        {
            return (T) Systems[typeof(T)];
        }
        
        private void AddSystem(ISystem system)
        {
            Systems.Add(system.GetType(), system);
        }

        public void OnClientConnected(object sender, EventArgs args)
        {
            Messages.Message($"Connected to server", MessageTypeDefOf.NeutralEvent, false);

            // Prompt to user to enable anticheat
            if (!Anticheat && Client.LegacySettings.Anticheat)
                AnticheatUtil.ShowEnableAnticheatDialog();

            // Inform mod systems that we're connected
            foreach (ISystem system in Systems.Values)
                system.OnConnected(Client);

            Client.MarkDirty();
        }

        public void ClientOnDisconnected(object sender, DisconnectedEventArgs e)
        {
            string key = "Rl_MessageDisconnected";
            switch (e.Reason)
            {
                case DisconnectReason.Error:
                    key = "Rl_MessageDisconnectedError";
                    break;
                case DisconnectReason.Kicked:
                    key = "Rl_MessageDisconnectedKicked";
                    break;
                case DisconnectReason.Network:
                    key = "Rl_MessageDisconnectedNetwork";
                    break;
            }

            string message = key.Translate();
            if (e.ReasonMessage != null)
                message += $" ({e.ReasonMessage})";
            
            Messages.Message(message, MessageTypeDefOf.NeutralEvent, false);
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();

            Client?.Update();
            
            // Systems update
            foreach (ISystem system in Systems.Values)
                system.Update();

            ConnectionManager.Update();

            // Send update every x seconds
            if (Time.realtimeSinceStartup - _lastUpdateSent >= UpdateInterval && RimLinkMod.Active)
            {
                _lastUpdateSent = Time.realtimeSinceStartup;
                Client?.MarkDirty(); // marking as dirty causes a new update to be sent
            }
        }

        public void OnPlayerConnected(object sender, Player e)
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

        public void OnPlayerUpdated(object sender, Client.PlayerUpdateEventArgs e)
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
            Scribe_Collections.Look(ref PlayerFactions, "player_factions", LookMode.Value, LookMode.Reference, ref _tmpPlayerFactionGuids, ref _tmpPlayerFactions);
            Scribe_Values.Look(ref Anticheat, "anticheat", false, true);

            // Expose systems
            foreach (KeyValuePair<Type, ISystem> kv in Systems)
            {
                Scribe.EnterNode(kv.Key.FullName);
                kv.Value.ExposeData();
                Scribe.ExitNode();
            }
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
