﻿using RimLink.Util;
using RimLink.Core;
using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Patches;
using RimLink.Systems.Mechanoids.Designer;
using RimLink.Systems.Mechanoids.Patches;
using RimWorld;
using Verse;

namespace RimLink.Systems.Mechanoids
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
                ReceiveCluster(mechPacket.Cluster, mechPacket.From.GuidToPlayer());
            }
        }
        
        public void ExposeData() {}

        public void Update() {}

        public static void ReceiveCluster(MechCluster cluster, Player from = null, Map map = null)
        {
            if (map == null) map = Find.AnyPlayerHomeMap;
            
            // Setting these values will cause the manually defined cluster to generate next time a cluster is generated
            Patch_MechClusterGenerator.Cluster = cluster;
            Patch_MechClusterGenerator.Map = map;
            Patch_MechClusterUtility.Cluster = cluster;
            
            // Now we manually invoke a mechanoid cluster
            var incidentParams = new IncidentParms
            {
                target = map,
                points = 0,
            };
            if (from != null)
            {
                incidentParams.customLetterDef = LetterDefOf.ThreatBig;
                incidentParams.customLetterLabel = "Rl_MechanoidCluster".Translate(from.Name);
                incidentParams.customLetterText = "Rl_MechanoidClusterDesc".Translate(from.Name.Colorize(from.Color.ToColor()));
            }
            
            IncidentDefOf.MechCluster.Worker.TryExecute(incidentParams);
        }

        public static bool IsCapableOfSending()
        {
            return ResearchProjectDefOf_Mechanoids.MechanoidCommunications.IsFinished;
        }

        public static float GetDiscount()
        {
            return ResearchProjectDefOf_Mechanoids.MechanoidRelations.IsFinished ? 0.2f : 0f;
        }

        [DefOf]
        public static class ResearchProjectDefOf_Mechanoids
        {
            public static ResearchProjectDef MechanoidCommunications;
            public static ResearchProjectDef MechanoidRelations;
        }
    }
}
