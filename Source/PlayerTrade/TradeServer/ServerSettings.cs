using PlayerTrade;

namespace TradeServer
{
    public class ServerSettings
    {
        public GameSettings GameSettings = new GameSettings();
        public int Port = 35562;
        public int MaxPlayers = 64;
        public bool LogPacketTraffic = true;
    }
}
