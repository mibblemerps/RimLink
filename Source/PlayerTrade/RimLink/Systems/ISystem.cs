using RimLink.Net;

namespace RimLink.Systems
{
    public interface ISystem
    {
        /// <summary>
        /// Called once the client instance has connected to the server.
        /// </summary>
        void OnConnected(Client client);

        void ExposeData();

        void Update();
    }
}