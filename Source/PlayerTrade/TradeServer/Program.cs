using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeServer
{
    public class Program
    {
        public static Server Server;

        private static void Main(string[] args)
        {
            Console.WriteLine("=== RimWorld Trade Server ===\n");

            int port = 35562;
            Console.WriteLine($"Starting server on port {port}...");
            Server = new Server();
            Server.Run(port).Wait();
        }
    }
}
