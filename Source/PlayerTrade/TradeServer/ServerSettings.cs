using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
