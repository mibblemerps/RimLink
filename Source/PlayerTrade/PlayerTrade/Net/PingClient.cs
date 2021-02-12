using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PlayerTrade.Net
{
    /// <summary>
    /// A simple client for simply exchanging a ping and ping response with the server.
    /// </summary>
    public class PingClient : Connection
    {
        public async Task<PacketPingResponse> Ping()
        {
            try
            {
                await SendPacketDirect(new PacketPing {ProtocolVersion = RimLinkMod.ProtocolVersion});
                PacketPingResponse response = (PacketPingResponse) await ReceivePacket();
                return response;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to ping server. " + e.Message + "\n" + e.StackTrace);
                Tcp?.Close();
                return null;
            }
        }
    }
}
