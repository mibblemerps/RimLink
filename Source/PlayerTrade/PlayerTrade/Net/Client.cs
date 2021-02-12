using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Chat;
using PlayerTrade.Labor;
using PlayerTrade.Mail;
using PlayerTrade.Mechanoids;
using PlayerTrade.Raids;
using PlayerTrade.Trade;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class Client : Connection
    {
        public event EventHandler Connected;
        public event EventHandler<PlayerUpdateEventArgs> PlayerUpdated;
        public event EventHandler<Player> PlayerConnected;
        public event EventHandler<Player> PlayerDisconnected;

        public new event EventHandler<PacketReceivedEventArgs> PacketReceived; 

        public delegate bool PacketPredicate(Packet packet);

        public RimLinkComp RimLinkComp;
        public LaborWorker Labor;
        public ChatWorker Chat;

        public ClientState State = ClientState.Disconnected;
        public Player Player { get; private set; }
        public string Guid => RimLinkComp.Guid; // Unique user ID

        public GameSettings GameSettings;

        public Dictionary<string, Player> OnlinePlayers = new Dictionary<string, Player>();
        
        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();

        private readonly List<AwaitPacketRequest> _awaitingPackets = new List<AwaitPacketRequest>();

        private Queue<Packet> _pendingPackets = new Queue<Packet>();
        
        public Client(RimLinkComp rimLinkComp)
        {
            RimLinkComp = rimLinkComp;
            MarkDirty(false, true);

            PacketReceived += OnPacketReceived;
            PlayerUpdated += OnPlayerUpdated;
            PlayerConnected += OnPlayerConnected;
            PlayerDisconnected += OnPlayerDisconnected;

            Labor = new LaborWorker(this);
            new MailWorker(this);
            Chat = new ChatWorker(this);
            new MechanoidWorker(this);
        }

        public async Task Connect(string ip, int port = 35562)
        {
            Tcp = new TcpClient();
            try
            {
                await Tcp.ConnectAsync(ip, port);
            }
            catch (Exception e)
            {
                throw new ConnectionFailedException(e.Message, true, e);
            }

            Stream = Tcp.GetStream();

            // Send connect request
            SendPacket(new PacketConnect
            {
                ProtocolVersion = RimLinkMod.ProtocolVersion,
                Guid = Guid,
                Secret = RimLinkComp.Secret,
                Player = Player
            });

            Run();
            
            // Await connection response
            PacketConnectResponse response = (PacketConnectResponse) await AwaitPacket(packet => packet is PacketConnectResponse, 2000);
            if (response == null)
            {
                Tcp.Close();
                throw new ConnectionFailedException("No connect response received. Is the server running and reachable?", true);
            }

            if (!response.Success)
            {
                Tcp.Close();
                throw new ConnectionFailedException("Server refused connection: " + response.FailReason, response.AllowReconnect);
            }

            Log.Message("Connected!");
            Log.Message($"GameSettings: RaidBasePrice={response.Settings.RaidBasePrice} MaxRaidStrength={response.Settings.RaidMaxStrengthPercent} Anticheat={response.Settings.Anticheat}");

            State = ClientState.Active;

            GameSettings = response.Settings;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public void Run()
        {
            _ = SendPackets();
            _ = ReceivePackets();
        }

        private async Task ReceivePackets()
        {
            while (Tcp.Connected)
            {
                try
                {
                    Packet packet = await ReceivePacket();
                    if (packet == null)
                        break;
                    _pendingPackets.Enqueue(packet);
                }
                catch (Exception e)
                {
                    Log.Error($"Error receiving packet ({e.GetType().Name})", e);
                    Disconnect();
                }
            }
        }

        private async Task SendPackets()
        {
            while (Tcp.Connected)
            {
                await SendQueuedPackets();
            }
        }

        public void Update()
        {
            while (_pendingPackets.Count > 0)
            {
                Packet packet = _pendingPackets.Dequeue();
                PacketReceived?.Invoke(this, new PacketReceivedEventArgs(Packet.Packets.First(p => p.Value == packet.GetType()).Key, packet));
            }
        }

        public void MarkDirty(bool sendPacket = true, bool mapIndependent = false)
        {
            Player = Player.Self(mapIndependent);
            OnlinePlayers[Guid] = Player; // add ourselves to the player list
            if (sendPacket && State == ClientState.Active)
                SendColonyInfo();
        }

        public Player GetPlayer(string guid)
        {
            foreach (Player player in GetPlayers(includeSelf: true))
            {
                if (player.Guid == guid)
                    return player;
            }

            return null;
        }

        public string GetName(string guid, bool colored = false)
        {
            Player player = GetPlayer(guid);
            if (player != null)
                return colored ? player.Name.Colorize(player.Color.ToColor()) : player.Name;

            // Fallback to just showing GUID
            return "{" + guid + "}";
        }

        public IEnumerable<Player> GetPlayers(bool online = false, bool includeSelf = false)
        {
            // Get online playres
            foreach (Player player in OnlinePlayers.Values)
            {
                if (!includeSelf && player.IsUs)
                    continue; // Skip self
                yield return player;
            }

            if (!online)
            {
                // Get offline players
                foreach (Player player in RimLinkComp.RememberedPlayers.Where(p => !p.IsOnline))
                {
                    if (OnlinePlayers.ContainsKey(player.Guid))
                        continue; // This player is online

                    yield return player;
                }
            }
        }

        public void SendColonyInfo()
        {
            SendPacket(new PacketColonyInfo
            {
                Guid = RimLinkComp.Find().Guid,
                Player = Player
            });
        }

        public async void SendTradeOffer(TradeOffer tradeOffer)
        {
            ActiveTradeOffers.Add(tradeOffer);

            Log.Message("Sending trade offer...");
            SendPacket(PacketTradeOffer.MakePacket(tradeOffer));
        }

        public void SendColonyResources()
        {
            try
            {
                var resources = new Resources();
                resources.Update(Find.CurrentMap);
                SendPacket(new PacketColonyResources(Guid, resources));
                Log.Message($"Sent resource info ({resources.Things.Count} things, {resources.Pawns.Count} pawns)");
            }
            catch (Exception e)
            {
                Log.Error("Exception sending colony resources!", e);
                throw;
            }
        }

        public async Task<Packet> AwaitPacket(PacketPredicate predicate, int timeout = 0)
        {
            var source = new TaskCompletionSource<Packet>();
            var request = new AwaitPacketRequest
            {
                Predicate = predicate,
                CompletionSource = source
            };
            _awaitingPackets.Add(request);

            if (timeout > 0)
            {
                if (await Task.WhenAny(source.Task, Task.Delay(timeout)) == source.Task)
                {
                    // Success
                    Packet result = await source.Task;
                    Log.Message($"Awaited packet received {result.GetType().Name}");
                    _awaitingPackets.Remove(request);
                    return result;
                }

                // Timed out
                return null;

            }
            else
            {
                // No timeout
                Packet result = await source.Task;
                Log.Message($"Awaited packet received {result.GetType().Name}");
                _awaitingPackets.Remove(request);
                return result;
            }
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            Log.Message($"Packet received #{e.Id} ({e.Packet.GetType().Name})");

            // Check awaiting packets
            while (_awaitingPackets.Count > 0)
            {
                var awaiting = _awaitingPackets.First();
                if (awaiting.Predicate(e.Packet))
                {
                    awaiting.CompletionSource?.TrySetResult(e.Packet);
                    _awaitingPackets.Remove(awaiting);
                }
            }

            try
            {
                switch (e.Id)
                {
                    case Packet.ColonyInfoId:
                        PacketColonyInfo infoPacket = (PacketColonyInfo) e.Packet;
                        //Log.Message($"Received colony info update for {infoPacket.Player.Name} (tradeable = {infoPacket.Player.TradeableNow})");
                        Player oldPlayer = OnlinePlayers.ContainsKey(infoPacket.Guid) ? OnlinePlayers[infoPacket.Guid] : null;
                        if (oldPlayer == null)
                        {
                            // New connection
                            Log.Message($"Player {infoPacket.Player.Name} connected");
                            PlayerConnected?.Invoke(this, infoPacket.Player);
                        }

                        OnlinePlayers[infoPacket.Guid] = infoPacket.Player;
                        PlayerUpdated?.Invoke(this, new PlayerUpdateEventArgs(oldPlayer, infoPacket.Player));
                        break;

                    case Packet.PlayerDisconnectedPacketId:
                        PacketPlayerDisconnected playerDisconnectedPacket = (PacketPlayerDisconnected) e.Packet;
                        if (OnlinePlayers.ContainsKey(playerDisconnectedPacket.Player))
                        {
                            var disconnectedPlayer = OnlinePlayers[playerDisconnectedPacket.Player];
                            OnlinePlayers.Remove(playerDisconnectedPacket.Player);
                            PlayerDisconnected?.Invoke(this, disconnectedPlayer);
                        }

                        break;

                    case Packet.RequestColonyResourcesId:
                        // Send server our current resources
                        Log.Message($"Fulfilling request for colony resources...");
                        SendColonyResources();
                        break;

                    case Packet.TradeOfferPacketId:
                        TradeOffer newTradeOffer = ((PacketTradeOffer) e.Packet).ToTradeOffer();
                        ActiveTradeOffers.Add(newTradeOffer);
                        TradeUtil.PresentTradeOffer(newTradeOffer);
                        break;

                    case Packet.AcceptTradePacketId:
                        await HandleAcceptTradePacket((PacketAcceptTrade) e.Packet);
                        break;

                    case Packet.ConfirmTradePacketId:
                        await HandleConfirmTradePacket((PacketTradeConfirm) e.Packet);
                        break;

                    case Packet.TriggerRaidPacketId:
                        PacketTriggerRaid raidPacket = (PacketTriggerRaid) e.Packet;
                        Log.Message($"Received raid from {GetName(raidPacket.Raid.From)}");
                        RimLinkComp.Instance.RaidsPending.Add(raidPacket.Raid);
                        raidPacket.Raid.InformTargetBountyPlaced();
                        break;

                    case Packet.AnnouncementPacketId:
                        AnnouncementUtility.Show((PacketAnnouncement) e.Packet);
                        break;

                    case Packet.GiveItemPacketId:
                        var giveItemPacket = (PacketGiveItem) e.Packet;
                        try
                        {
                            giveItemPacket.GiveItem();

                            SendPacket(new PacketAcknowledgement
                            {
                                Guid = giveItemPacket.Reference,
                                Success = true
                            });
                        }
                        catch (Exception giveException)
                        {
                            SendPacket(new PacketAcknowledgement
                            {
                                Guid = giveItemPacket.Reference,
                                Success = false,
                                FailReason = giveException.Message
                            });
                        }

                        break;

                    case Packet.RequestBugReportPacketId:
                        BugReport.Send("Bug report requested via command.");
                        break;

                    case Packet.KickPacketId:
                        PacketKick kickPacket = (PacketKick) e.Packet;
                        
                        if (!kickPacket.AllowReconnect) // Disable auto reconnect
                            RimLinkComp.ReconnectOnNextDisconnect = false;

                        if (kickPacket.Reason != null)
                        {
                            // Show reason
                            Find.WindowStack.Add(new Dialog_MessageBox(kickPacket.Reason, title: "Kicked", buttonAText: "Close"));
                        }
                        
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception handling packet! ({e.Packet.GetType().Name})", ex);
            }
        }

        private void OnPlayerUpdated(object sender, PlayerUpdateEventArgs e)
        {
            // If this is a new player, or if their tradeable status has changed - issue a message
            if (e.OldPlayer == null || e.Player.TradeableNow != e.OldPlayer.TradeableNow)
            {
                if (e.Player.TradeableNow)
                {
                    Log.Message($"{e.Player.Name} is now tradable");
                    Messages.Message($"{e.Player.Name} is now tradable", def: MessageTypeDefOf.NeutralEvent, false);

                }
                else if (e.OldPlayer != null) // only show this message if the player previously existed
                {
                    Log.Message($"{e.Player.Name} no longer tradable");
                    Messages.Message($"{e.Player.Name} is no longer tradable", def: MessageTypeDefOf.NeutralEvent, false);
                }
            }
        }

        private void OnPlayerDisconnected(object sender, Player e)
        {
            Messages.Message($"{e.Name.Colorize(ColoredText.FactionColor_Neutral)} disconnected", MessageTypeDefOf.NeutralEvent, false);
        }

        private void OnPlayerConnected(object sender, Player e)
        {
            Messages.Message($"{e.Name.Colorize(ColoredText.FactionColor_Neutral)} connected", MessageTypeDefOf.NeutralEvent, false);
        }

        private async Task HandleAcceptTradePacket(PacketAcceptTrade packet)
        {
            bool confirm = packet.Accept;

            TradeOffer acceptOffer = ActiveTradeOffers.FirstOrDefault(tradeOffer => tradeOffer.Guid == packet.Trade);
            if (acceptOffer == null)
            {
                // Offer not found (probably expired) - send rejection
                SendPacket(new PacketTradeConfirm
                {
                    Trade = packet.Trade,
                    Confirm = false
                });
                return;
            }

            if (acceptOffer.IsForUs)
            {
                // Trade not for us (it'd be a trade we've sent)
                Log.Warn("Received a offer acceptance for a trade we received. This isn't right, we're supposed to be the one sending the acceptance!");
                confirm = false;
            }

            if (!acceptOffer.CanFulfill(false))
            {
                // We cannot fulfill the trade
                Messages.Message($"Unable to fulfill trade for {acceptOffer.For} - missing resources.", MessageTypeDefOf.NeutralEvent, false);
                confirm = false;
            }

            if (packet.Accept) // don't send confirmation/rejection if the other party declined the trade, no point confirming a rejected trade
            {
                // Send confirmation/rejection
                SendPacket(new PacketTradeConfirm
                {
                    Trade = packet.Trade,
                    Confirm = confirm
                });
            }

            if (confirm)
            {
                RimLinkComp.TradeOffersPendingFulfillment.Add(acceptOffer);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Trade Rejected ({RimLinkComp.Find().Client.GetName(acceptOffer.For)})", $"{acceptOffer.For} rejected your trade offer.", LetterDefOf.NeutralEvent);
            }

            acceptOffer.TradeAccepted?.TrySetResult(confirm);

            // Remove trade offer
            ActiveTradeOffers.Remove(acceptOffer);
        }

        private async Task HandleConfirmTradePacket(PacketTradeConfirm packet)
        {
            TradeOffer offer = ActiveTradeOffers.FirstOrDefault(tradeOffer => tradeOffer.Guid == packet.Trade);

            bool confirm = packet.Confirm;

            if (offer == null)
            {
                // Offer not found (probably expired).
                return;
            }

            if (!offer.IsForUs)
            {
                // Trade not for us (it'd be a trade we've sent)
                Log.Warn("Received a offer confirmation for a trade we sent! We're supposed to be the ones sending the trade confirmation!");
                confirm = false;
            }

            if (confirm)
            {
                RimLinkComp.TradeOffersPendingFulfillment.Add(offer);
            }
            else
            {
                Find.LetterStack.ReceiveLetter($"Trade Failed ({RimLinkComp.Find().Client.GetName(offer.From)})", $"Trade from {RimLinkComp.Find().Client.GetName(offer.From)} is no longer available.", LetterDefOf.NeutralEvent);
            }

            offer.TradeAccepted?.TrySetResult(confirm);

            ActiveTradeOffers.Remove(offer);
        }

        public class AwaitPacketRequest
        {
            public PacketPredicate Predicate;
            public TaskCompletionSource<Packet> CompletionSource;
        }

        public class PlayerUpdateEventArgs : EventArgs
        {
            public Player OldPlayer;
            public Player Player;

            public PlayerUpdateEventArgs(Player oldPlayer, Player player)
            {
                OldPlayer = oldPlayer;
                Player = player;
            }
        }

        public enum ClientState
        {
            Disconnected,
            Active
        }
    }
}
