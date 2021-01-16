namespace TradeServer.Commands
{
    public abstract class Caller
    {
        public abstract string Guid { get; }
        public abstract bool IsAdmin { get; }

        public abstract void Output(string output);
        public abstract void Error(string error);
    }
}
