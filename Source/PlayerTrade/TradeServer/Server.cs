using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade;
using PlayerTrade.Net;

namespace TradeServer
{
    public class Server
    {
        public static int ProtocolVersion = 1;

        public List<Client> AuthenticatedClients = new List<Client>();

        protected TcpListener Listener;

        public async Task Run(int port = 35562)
        {
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();

            while (true)
            {
                TcpClient tcp = await Listener.AcceptTcpClientAsync();
                Console.WriteLine($"Accepted TCP connection from {tcp.Client.RemoteEndPoint}");
                var client = new Client(tcp);
                client.Authenticated += ClientOnAuthenticated;
                client.Disconnected += ClientOnDisconnected;
                client.ColonyInfoReceived += ClientOnColonyInfoReceived;
                client.Run();
            }
        }

        public Client GetClient(string guid)
        {
            return AuthenticatedClients.FirstOrDefault(client => client.Player.Guid == guid);
        }

        public string GetName(string guid)
        {
            var client = GetClient(guid);
            if (client == null || client.Player == null || client.Player.Name == null)
                return "{" + guid + "}";
            return client.Player.Name;
        }

        private async void ClientOnAuthenticated(object sender, Client.ClientEventArgs e)
        {
            Console.WriteLine($"{e.Client.Player.Name} connected");
            AuthenticatedClients.Add(e.Client);

            // Send other colonies
            foreach (Client client in AuthenticatedClients)
            {
                if (client.Player.Guid == e.Client.Player.Guid)
                    continue; // Skip self

                await e.Client.SendPacket(new PacketColonyInfo
                {
                    Guid = client.Player.Guid,
                    Player = client.Player
                });
            }
        }

        private void ClientOnDisconnected(object sender, Client.ClientEventArgs e)
        {
            if (e.Client.Player != null)
                Console.WriteLine($"{e.Client.Player.Name} disconnected");
            AuthenticatedClients.Remove(e.Client);
        }

        private void ClientOnColonyInfoReceived(object sender, Client.ClientEventArgs e)
        {
            foreach (Client client in AuthenticatedClients)
            {
                if (client.Player.Guid == e.Client.Player.Guid)
                    continue; // Skip ourselves

                _ = client.SendPacket(new PacketColonyInfo
                {
                    Guid = e.Client.Player.Guid,
                    Player = e.Client.Player
                });
            }
        }
    }
}
