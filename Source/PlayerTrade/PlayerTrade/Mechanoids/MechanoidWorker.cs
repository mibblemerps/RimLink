using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;

namespace PlayerTrade.Mechanoids
{
    public class MechanoidWorker
    {
        public Client Client;

        public MechanoidWorker(Client client)
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
    }
}
