using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Mechanoids
{
    public class MechanoidSystem : ISystem
    {
        public Client Client;

        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketMechanoidCluster mechPacket)
            {
                Log.Message($"Mechanoid cluster from {mechPacket.From}. Parts = {mechPacket.Cluster.Parts.Count}");
            }
        }
        
        public void ExposeData() {}

        public void Update() {}
    }
}
