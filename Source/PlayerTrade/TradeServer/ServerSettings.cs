using PlayerTrade;

namespace TradeServer
{
    public class ServerSettings
    {
        public GameSettings GameSettings = new GameSettings();
        public int MaxPlayers = 64;
        public bool LogPacketTraffic = true;
    }
}
