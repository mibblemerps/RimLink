using PlayerTrade;

namespace TradeServer
{
    public class ServerSettings
    {
        public GameSettings GameSettings = new GameSettings();
        public int Port = 35565;
        public int MaxPlayers = 64;
        public bool LogPacketTraffic = true;
    }
}
