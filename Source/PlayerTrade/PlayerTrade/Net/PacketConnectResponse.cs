using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    public class PacketConnectResponse : Packet
    {
        /// <summary>
        /// Was the connection successful? If false the connection will be dropped momentarily.
        /// </summary>
        public bool Success;

        /// <summary>
        /// (If connection failed) A string explaining why the connection failed.
        /// </summary>
        public string FailReason;

        /// <summary>
        /// (If connection failed) Whether the client should try to automatically reconnect.
        /// </summary>
        public bool AllowReconnect;

        /// <summary>
        /// List of players already connected to the server.
        /// </summary>
        public List<Player> ConnectedPlayers = new List<Player>();
        
        /// <summary>
        /// Current game settings.
        /// </summary>
        public GameSettings Settings;

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteBoolean(Success);
            buffer.WriteString(FailReason, true);
            buffer.WriteBoolean(AllowReconnect);
            buffer.WriteList(ConnectedPlayers, (b, i) => b.WritePacketable(i));

            if (Settings == null)
            {
                buffer.WriteBoolean(false);
            }
            else
            {
                buffer.WriteBoolean(true);
                buffer.Write(Settings);
            }
        }

        public override void Read(PacketBuffer buffer)
        {
            Success = buffer.ReadBoolean();
            FailReason = buffer.ReadString(true);
            AllowReconnect = buffer.ReadBoolean();
            ConnectedPlayers = buffer.ReadList<Player>(b => b.ReadPacketable<Player>());

            if (buffer.ReadBoolean())
                Settings = buffer.Read<GameSettings>();
        }
    }
}
