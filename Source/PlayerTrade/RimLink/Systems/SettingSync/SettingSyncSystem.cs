using RimLink.Net;
using RimLink.Net.Packets;
using RimLink.Systems.SettingSync.Packets;
using Verse;

namespace RimLink.Systems.SettingSync
{
    public class SettingSyncSystem : ISystem
    {
        public InGameSettings Settings;
        
        protected Client Client;

        public void OnConnected(Client client)
        {
            Client = client;
            Client.PacketReceived += OnPacketReceived;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref Settings, "settings");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Settings == null)
                    Settings = new InGameSettings();
                Settings.Apply();
            }
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet is PacketSyncSettings packetSettings)
            {
                Settings = packetSettings.Settings.Value;
                Settings.Apply();
            }
        }
        
        public void Update() {}
    }
}