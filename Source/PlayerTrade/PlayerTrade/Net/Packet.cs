using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Artifacts;
using PlayerTrade.Chat;
using PlayerTrade.Labor.Packets;
using PlayerTrade.Mail;
using PlayerTrade.Mechanoids;

namespace PlayerTrade.Net
{
    public abstract class Packet : IPacketable
    {
        public const int ConnectId = 1;
        public const int ColonyResourcesId = 2;
        public const int ColonyInfoId = 3;
        public const int InitiateTradeId = 4;
        public const int RequestColonyResourcesId = 5;
        public const int TradeOfferPacketId = 6;
        public const int AcceptTradePacketId = 7;
        public const int ConfirmTradePacketId = 8;
        public const int TriggerRaidPacketId = 9;
        public const int LaborOfferPacketId = 11;
        public const int AcceptLaborOfferPacketId = 12;
        public const int ConfirmLaborOfferPacketId = 13;
        public const int ReturnLentColonistsPacketId = 14;
        public const int PlayerDisconnectedPacketId = 15;
        public const int ConnectResponsePacketId = 16;
        public const int MailPacketId = 17;
        public const int ArtifactPacketId = 18;
        public const int AnnouncementPacketId = 19;
        public const int SendChatMessagePacketId = 20;
        public const int ReceiveChatMessagePacketId = 21;
        public const int GiveItemPacketId = 22;
        public const int AcknowledgementPacketId = 23;
        public const int MechanoidClusterPacketId = 24;
        public const int BugReportPacketId = 25;
        public const int RequestBugReportPacketId = 26;
        public const int KickPacketId = 27;

        public static Dictionary<int, Type> Packets = new Dictionary<int, Type>
        {
            {ConnectId, typeof(PacketConnect)},
            {ColonyResourcesId, typeof(PacketColonyResources)},
            {ColonyInfoId, typeof(PacketColonyInfo)},
            {InitiateTradeId, typeof(PacketInitiateTrade)},
            {RequestColonyResourcesId, typeof(PacketRequestColonyResources)},
            {TradeOfferPacketId, typeof(PacketTradeOffer)},
            {AcceptTradePacketId, typeof(PacketAcceptTrade)},
            {ConfirmTradePacketId, typeof(PacketTradeConfirm)},
            {TriggerRaidPacketId, typeof(PacketTriggerRaid)},
            {LaborOfferPacketId, typeof(PacketLaborOffer)},
            {AcceptLaborOfferPacketId, typeof(PacketAcceptLaborOffer)},
            {ConfirmLaborOfferPacketId, typeof(PacketConfirmLaborOffer)},
            {ReturnLentColonistsPacketId, typeof(PacketReturnLentColonists)},
            {PlayerDisconnectedPacketId, typeof(PacketPlayerDisconnected)},
            {ConnectResponsePacketId, typeof(PacketConnectResponse)},
            {MailPacketId, typeof(PacketMail)},
            {ArtifactPacketId, typeof(PacketArtifact)},
            {AnnouncementPacketId, typeof(PacketAnnouncement)},
            {SendChatMessagePacketId, typeof(PacketSendChatMessage)},
            {ReceiveChatMessagePacketId, typeof(PacketReceiveChatMessage)},
            {GiveItemPacketId, typeof(PacketGiveItem)},
            {AcknowledgementPacketId, typeof(PacketAcknowledgement)},
            {MechanoidClusterPacketId, typeof(PacketMechanoidCluster)},
            {BugReportPacketId, typeof(PacketBugReport)},
            {RequestBugReportPacketId, typeof(PacketRequestBugReport)},
            {KickPacketId, typeof(PacketKick)},
        };

        public abstract void Write(PacketBuffer buffer);
        public abstract void Read(PacketBuffer buffer);
    }
}
