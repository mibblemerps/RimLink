using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketConnectResponse : Packet
    {
        public bool Success;
        public string FailReason;
        public List<Player> ConnectedPlayers = new List<Player>();
        public GameSettings Settings;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteBoolean(Success);
            buffer.WriteString(FailReason, true);
            buffer.WriteInt(ConnectedPlayers.Count);
            foreach (var connectedPlayer in ConnectedPlayers)
                buffer.Write(connectedPlayer);
            buffer.Write(Settings);
        }

        public override void Read(PacketBuffer buffer)
        {
            Success = buffer.ReadBoolean();
            FailReason = buffer.ReadString(true);
            int playerCount = buffer.ReadInt();
            ConnectedPlayers = new List<Player>(playerCount);
            for (int i = 0; i < playerCount; i++)
                ConnectedPlayers.Add(buffer.Read<Player>());
            Settings = buffer.Read<GameSettings>();
        }
    }
}
