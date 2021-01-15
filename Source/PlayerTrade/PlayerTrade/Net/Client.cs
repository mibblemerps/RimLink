using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Labor;
using PlayerTrade.Mail;
using PlayerTrade.Raids;
using PlayerTrade.Trade;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class Client : Connection
    {
        public event EventHandler<PlayerUpdateEventArgs> PlayerUpdated;
        public event EventHandler<Player> PlayerConnected;
        public event EventHandler<Player> PlayerDisconnected;

        public delegate bool PacketPredicate(Packet packet);

        public RimLinkComp RimLinkComp;
        public LaborWorker Labor;

        public Player Player { get; private set; }

        public GameSettings GameSettings;
        public Dictionary<string, Player> Players = new Dictionary<string, Player>();

        public string Guid => RimLinkComp.Guid; // Unique user ID

        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();

        public Dictionary<PacketPredicate, TaskCompletionSource<Packet>> AwaitingPackets = new Dictionary<PacketPredicate, TaskCompletionSource<Packet>>();

        private TaskCompletionSource<PacketColonyResources> _awaitColonyResources = new TaskCompletionSource<PacketColonyResources>();

        public bool IsTradableNow
        {
            get => _isTradableNow;
            set
            {
                _isTradableNow = value;
                MarkDirty();
            }
        }

        private Task _clientTcpTask;

        private bool _isTradableNow;

        public Client(RimLinkComp rimLinkComp)
        {
            RimLinkComp = rimLinkComp;
            MarkDirty(false, true);

            PlayerUpdated += OnPlayerUpdated;
            PlayerConnected += OnPlayerConnected;
            PlayerDisconnected += OnPlayerDisconnected;

            Labor = new LaborWorker(this);
            new MailWorker(this);
        }

        public async Task Connect(string ip, int port = 35562)
        {
            Tcp = new TcpClient();
            await Tcp.ConnectAsync(ip, port);
            Stream = Tcp.GetStream();

            PacketReceived += OnPacketReceived;

            // Send connect request
            await SendPacket(new PacketConnect
            {
                ProtocolVersion = 1,
                Guid = Guid,
                Secret = RimLinkComp.Secret,
                Player = Player
            });

            _clientTcpTask = Run();

            // Await connection response
            PacketConnectResponse response = (PacketConnectResponse) await AwaitPacket(packet => packet is PacketConnectResponse, 1000);
            if (response == null)
            {
                Tcp.Close();
                throw new Exception("No connect response received. Is the server running and reachable?");
            }

            if (!response.Success)
            {
                Tcp.Close();
                throw new Exception("Server refused connection: " + response.FailReason);
            }

            GameSettings = response.Settings;
        }

        public async Task Run()
        {
            while (Tcp.Connected)
            {
                try
                {
                    await ReceivePacket();
                }
                catch (ObjectDisposedException) {} // don't care about object disposed exceptions, this just means the connection is closed now
                catch (Exception e)
                {
                    Log.Error("Error receiving packet", e);
                }
            }

            Log.Message($"Disconnected from Trade server");
        }

        public void MarkDirty(bool sendPacket = true, bool mapIndependent = false)
        {
            Player = Player.Self(mapIndependent);
            if (sendPacket)
                _ = SendColonyInfo();
        }

        public string GetName(string guid)
        {
            if (Players.ContainsKey(guid))
                return Players[guid].Name;
            return "{" + guid + "}";
        }

        public async Task SendColonyInfo()
        {
            var colonyInfo = new PacketColonyInfo
            {
                Guid = RimLinkComp.Find().Guid,
                Player = Player
            };
            await SendPacket(colonyInfo);
        }

        /// <summary>
        /// Send the current trade deal
        /// </summary>
        /// <returns></returns>
        public async Task SendTradeOffer()
        {
            TradeOffer tradeOffer = TradeUtil.FormTradeOffer();
            ActiveTradeOffers.Add(tradeOffer);

            Log.Message("Forming trade packet...");
            PacketTradeOffer tradeReqPacket = null;
            try
            {
                tradeReqPacket = PacketTradeOffer.MakePacket(tradeOffer);
            }
            catch (Exception e)
            {
                Log.Error("Exception making trade offer packet", e);
                return;
            }

            Log.Message("Sending trade offer...");
            await SendPacket(tradeReqPacket);
            Log.Message("Trade offer sent");
        }

        public async Task<PacketColonyResources> GetColonyResources(Player player)
        {
            // Send trade request packet
            await SendPacket(new PacketInitiateTrade
            {
                Guid = player.Guid
            });

            // Await response
            // todo: use AwaitPacket
            PacketColonyResources packet = await _awaitColonyResources.Task;
            _awaitColonyResources = new TaskCompletionSource<PacketColonyResources>(); // reset task completion source for future requests
            return packet;
        }

        public async Task SendColonyResources()
        {
            var resources = new Resources();
            resources.Update(Find.CurrentMap);
            await SendPacket(new PacketColonyResources(Guid, resources));
        }

        public async Task<Packet> AwaitPacket(PacketPredicate predicate, int timeout = 0)
        {
            var source = new TaskCompletionSource<Packet>();
            AwaitingPackets.Add(predicate, source);

            if (timeout > 0)
            {
                if (await Task.WhenAny(source.Task, Task.Delay(timeout)) == source.Task)
                {
                    // Success
                    Packet result = await source.Task;
                    AwaitingPackets.Remove(predicate);
                    return result;
                }

                // Timed out
                return null;

            }
            else
            {
                // No timeout
                Packet result = await source.Task;
                AwaitingPackets.Remove(predicate);
                return result;
            }
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            Log.Message($"Packet received #{e.Id} ({e.Packet.GetType().Name})");

            // Check awaiting packets
            var toRemove = new List<PacketPredicate>();
            foreach (var awaiting in AwaitingPackets)
            {
                if (awaiting.Key(e.Packet))
                    awaiting.Value.TrySetResult(e.Packet);
                toRemove.Add(awaiting.Key);
            }
            foreach (var predicate in toRemove)
                AwaitingPackets.Remove(predicate);

            switch (e.Id)
            {
                case Packet.ColonyInfoId:
                    PacketColonyInfo infoPacket = (PacketColonyInfo) e.Packet;
                    //Log.Message($"Received colony info update for {infoPacket.Player.Name} (tradeable = {infoPacket.Player.TradeableNow})");
                    Player oldPlayer = Players.ContainsKey(infoPacket.Guid) ? Players[infoPacket.Guid] : null;
                    if (oldPlayer == null)
                    {
                        // New connection
                        Log.Message($"Player {infoPacket.Player.Name} connected");
                        PlayerConnected?.Invoke(this, infoPacket.Player);
                    }
                    Players[infoPacket.Guid] = infoPacket.Player;
                    PlayerUpdated?.Invoke(this, new PlayerUpdateEventArgs(oldPlayer, infoPacket.Player));
                    break;

                case Packet.PlayerDisconnectedPacketId:
                    PacketPlayerDisconnected playerDisconnectedPacket = (PacketPlayerDisconnected) e.Packet;
                    if (Players.ContainsKey(playerDisconnectedPacket.Player))
                    {
                        var disconnectedPlayer = Players[playerDisconnectedPacket.Player];
                        Players.Remove(playerDisconnectedPacket.Player);
                        PlayerDisconnected?.Invoke(this, disconnectedPlayer);
                    }
                    break;

                case Packet.ColonyResourcesId:
                    PacketColonyResources resourcesPacket = (PacketColonyResources) e.Packet;
                    _awaitColonyResources.TrySetResult(resourcesPacket);
                    break;

                case Packet.RequestColonyResourcesId:
                    // Send server our current resources
                    Log.Message($"Fulfilling request for colony resources...");
                    await SendColonyResources();
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
                    RimLinkComp.Find().RaidsPending.Add(raidPacket.Raid);
                    raidPacket.Raid.InformTargetBountyPlaced();
                    await SendPacket(new PacketRaidAccepted
                    {
                        For = raidPacket.Raid.From,
                        Id = raidPacket.Raid.Id
                    });
                    break;
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
                await SendPacket(new PacketTradeConfirm
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
                await SendPacket(new PacketTradeConfirm
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
    }
}
