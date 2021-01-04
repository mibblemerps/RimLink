using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public abstract class Packet
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
        public const int RaidAcceptedPacketId = 10;

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
            {RaidAcceptedPacketId, typeof(PacketRaidAccepted)}
        };

        public abstract void Write(PacketBuffer buffer);
        public abstract void Read(PacketBuffer buffer);
    }
}
