using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Raids
{
    public class RaidSystem
    {
        public Client Client;

        public RaidSystem(Client client)
        {
            Client = client;
            Client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketTriggerRaid raidPacket)
            {
                Log.Message($"Received raid from {Client.GetName(raidPacket.Raid.From)}");
                RimLinkComp.Instance.RaidsPending.Add(raidPacket.Raid);
                raidPacket.Raid.InformTargetBountyPlaced();
            }
        }
    }
}
