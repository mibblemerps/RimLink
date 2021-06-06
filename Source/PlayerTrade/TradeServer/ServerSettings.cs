using PlayerTrade;

namespace TradeServer
{
    public class ServerSettings
    {
        public LegacySettings GameSettings = new LegacySettings();
        public int Port = 35562;
        public int MaxPlayers = 64;
        public bool LogPacketTraffic = true;
    }
}
