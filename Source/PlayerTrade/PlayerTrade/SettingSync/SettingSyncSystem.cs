using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.SettingSync.Packets;
using Verse;

namespace PlayerTrade.SettingSync
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
            if (Scribe.mode == LoadSaveMode.LoadingVars && Settings == null)
            {
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