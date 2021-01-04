using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Raids;
using RimWorld;
using Verse;

namespace PlayerTrade.Net
{
    public class Client : Connection
    {
        public event EventHandler<PlayerUpdateEventArgs> PlayerUpdated;

        public delegate bool PacketPredicate(Packet packet);

        public RimLinkComp RimLinkComp;

        public Player Player { get; private set; }

        public Dictionary<string, Player> Players = new Dictionary<string, Player>();

        public string Guid => RimLinkComp.Guid; // Unique user ID

        public List<TradeOffer> ActiveTradeOffers = new List<TradeOffer>();
        public List<TradeOffer> OffersToFulfillNextTick = new List<TradeOffer>();

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

        private bool _isTradableNow;

        public Client(RimLinkComp rimLinkComp)
        {
            RimLinkComp = rimLinkComp;
            MarkDirty(false);

            PlayerUpdated += OnPlayerUpdated;
        }

        public async Task Connect(string ip, int port = 35562)
        {
            Tcp = new TcpClient();
            await Tcp.ConnectAsync(ip, port);
            Stream = Tcp.GetStream();

            PacketReceived += OnPacketReceived;

            await SendPacket(new PacketConnect
            {
                ProtocolVersion = 1,
                Guid = Guid,
                Secret = RimLinkComp.Secret,
                Player = Player
            });

            Run();
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

        public void MarkDirty(bool sendPacket = true)
        {
            Player = Player.Self();
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

        public async Task<Packet> AwaitPacket(PacketPredicate predicate)
        {
            var source = new TaskCompletionSource<Packet>();
            AwaitingPackets.Add(predicate, source);
            return await source.Task;
        }

        private async void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            foreach (var awaiting in AwaitingPackets)
            {
                if (awaiting.Key(e.Packet))
                    awaiting.Value.TrySetResult(e.Packet);
                AwaitingPackets.Remove(awaiting.Key);
            }

            switch (e.Id)
            {
                case Packet.ColonyInfoId:
                    PacketColonyInfo infoPacket = (PacketColonyInfo) e.Packet;
                    Log.Message($"Received colony info update for {infoPacket.Player.Name}");
                    Player oldPlayer = Players.ContainsKey(infoPacket.Guid) ? Players[infoPacket.Guid] : null;
                    Players[infoPacket.Guid] = infoPacket.Player;
                    PlayerUpdated?.Invoke(this, new PlayerUpdateEventArgs(oldPlayer, infoPacket.Player));
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
                    Messages.Message($"{e.Player.Name} is now tradable", def: MessageTypeDefOf.NeutralEvent, historical: false);

                }
                else
                {
                    Log.Message($"{e.Player.Name} no longer tradable");
                    Messages.Message($"{e.Player.Name} is no longer tradable", def: MessageTypeDefOf.NeutralEvent, historical: false);
                }
            }
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
                OffersToFulfillNextTick.Add(acceptOffer);
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
                OffersToFulfillNextTick.Add(offer);
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
