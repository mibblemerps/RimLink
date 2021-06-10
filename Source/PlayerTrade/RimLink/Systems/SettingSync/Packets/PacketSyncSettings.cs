using RimLink.Net;
using RimLink.Net.Packets;

namespace RimLink.Systems.SettingSync.Packets
{
    /// <summary>
    /// <p>Used to sync in-game settings across clients.</p>
    /// <p>When sent Server -> Client, the settings are applied locally.</p>
    /// <p>When sent Client -> Server, it is treated as a setting changed is then relayed to all other clients (if the sending player is admin)</p>
    /// </summary>
    [Packet]
    public class PacketSyncSettings : Packet
    {
        public SerializedScribe<InGameSettings> Settings;
        
        public override void Write(PacketBuffer buffer)
        {
            buffer.WritePacketable(Settings);
        }

        public override void Read(PacketBuffer buffer)
        {
            Settings = buffer.ReadPacketable<SerializedScribe<InGameSettings>>();
        }
    }
}