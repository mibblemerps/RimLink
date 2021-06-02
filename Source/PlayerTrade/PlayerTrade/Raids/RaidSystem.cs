using System.Collections.Generic;
using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Util;
using RimWorld;
using Verse;

namespace PlayerTrade.Raids
{
    public class RaidSystem : ISystem
    {
        public List<BountyRaid> RaidsPending = new List<BountyRaid>();
        
        public Client Client;
        
        public void OnConnected(Client client)
        {
            Client = client;
            client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketTriggerRaid raidPacket)
            {
                Log.Message($"Received raid from {Client.GetName(raidPacket.Raid.From)}");
                RaidsPending.Add(raidPacket.Raid);
                raidPacket.Raid.InformTargetBountyPlaced();
            }
        }
        
        public void ExposeData()
        {
            Scriber.Collection(ref RaidsPending, "raids_pending");            
        }

        public void Update()
        {
            // Trigger pending raids
            var raidsToRemove = new List<BountyRaid>();
            foreach (BountyRaid raid in RaidsPending)
            {
                if (--raid.ArrivesInTicks <= 0)
                {
                    // Trigger raid
                    raid.Execute();
                    raidsToRemove.Add(raid);
                }
            }
            foreach (BountyRaid raid in raidsToRemove)
                RaidsPending.Remove(raid);
        }

        public static float GetDiscount()
        {
            if (ResearchProjectDefOf_Raid.NativeCulture.IsFinished)
                return 0.3f;
            if (ResearchProjectDefOf_Raid.NativeLanguages.IsFinished)
                return 0.15f;
            return 0f;
        }

        [DefOf]
        public static class ResearchProjectDefOf_Raid
        {
            public static ResearchProjectDef NativeLanguages;
            public static ResearchProjectDef NativeCulture;
        }
    }
}
