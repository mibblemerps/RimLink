using RimLink;

namespace TradeServer.Commands
{
    public class ServerCaller : Caller
    {
        public override string Guid => "Server";
        public override bool IsAdmin => true;

        public override void Output(string output)
        {
            Log.Message(output);
        }

        public override void Error(string error)
        {
            Log.Error(error);
        }
    }
}
