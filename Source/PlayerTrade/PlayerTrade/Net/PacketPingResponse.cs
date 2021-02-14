using System.Collections.Generic;

namespace PlayerTrade.Net
{
    public class PacketPingResponse : Packet
    {
        public int ProtocolVersion;
        public string ServerName;
        public int MaxPlayers;
        public List<Player> PlayersOnline = new List<Player>();

        public override void Write(PacketBuffer buffer)
        {
            buffer.WriteInt(ProtocolVersion);
            buffer.WriteString(ServerName);
            buffer.WriteInt(MaxPlayers);
            buffer.WriteList(PlayersOnline, (b, i) => b.WritePacketable(i));
        }

        public override void Read(PacketBuffer buffer)
        {
            ProtocolVersion = buffer.ReadInt();
            ServerName = buffer.ReadString();
            MaxPlayers = buffer.ReadInt();
            PlayersOnline = buffer.ReadList<Player>(b => b.ReadPacketable<Player>());
        }
    }
}
