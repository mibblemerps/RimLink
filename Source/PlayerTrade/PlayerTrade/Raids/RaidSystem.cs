using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Raids
{
    public class RaidSystem : ISystem
    {
        public Client Client;

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        public void ExposeData()
        {
            
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

        public void Update() {}
    }
}
