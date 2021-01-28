using System.Threading.Tasks;

namespace TradeServer.Commands
{
    public abstract class Command
    {
        protected Server Server => Program.Server;

        public abstract string Name { get; }
        public abstract string Usage { get; }

        public abstract Task Execute(Caller caller, string[] args);
    }
}
