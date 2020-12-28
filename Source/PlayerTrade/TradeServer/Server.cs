using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;

namespace TradeServer
{
    public class Server
    {
        public static int ProtocolVersion = 1;

        public List<Client> Clients = new List<Client>();

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
                client.TradableChanged += ClientOnTradableChanged;
                Clients.Add(client);
                client.Run();
            }
        }

        public Client GetClient(string username)
        {
            return Clients.FirstOrDefault(client => client.Username == username);
        }

        private async void ClientOnAuthenticated(object sender, Client.ClientEventArgs e)
        {
            Console.WriteLine($"{e.Client.Username} connected");

            // Send client tradable colonies
            foreach (Client client in Clients)
            {
                if (client.Username == e.Client.Username)
                    continue; // Skip self

                if (client.IsTradableNow)
                {
                    await e.Client.SendPacket(new PacketColonyTradable
                    {
                        TradableNow = true,
                        Username = client.Username
                    });
                }
            }
        }

        private void ClientOnDisconnected(object sender, Client.ClientEventArgs e)
        {
            Console.WriteLine($"{e.Client.Username} disconnected");
            Clients.Remove(e.Client);
        }

        private void ClientOnTradableChanged(object sender, Client.ClientEventArgs e)
        {
            Console.WriteLine($"Colony {e.Client.Username} tradable:\t{(e.Client.IsTradableNow ? "Yes" : "No")}");

            // Send tradable change to clients so they know that they can/cannot trade with this person
            foreach (Client client in Clients)
            {
                if (client.Username == e.Client.Username)
                    continue; // Don't send tradable change to themselves

                _ = client.SendPacket(new PacketColonyTradable()
                {
                    TradableNow = e.Client.IsTradableNow,
                    Username = e.Client.Username
                });
            }
        }
    }
}
