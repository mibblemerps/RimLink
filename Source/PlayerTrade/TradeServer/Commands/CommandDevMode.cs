using System.Threading.Tasks;
using PlayerTrade.Net.Packets;

namespace TradeServer.Commands
{
    public class CommandDevMode : Command
    {
        public override string Name => "devmode";
        
        public override string Usage => "<target> (enable?)";
        
        public override async Task Execute(Caller caller, string[] args)
        {
            CommandUtility.AdminRequired(caller);
            
            if (args.Length < 1)
                throw new CommandUsageException(this);

            bool enable = true;
            if (args.Length >= 2)
                enable = CommandUtility.ParseBoolean(args[1]);

            PacketDevMode packet = new PacketDevMode {Enable = enable};
            
            foreach (Client client in CommandUtility.GetClientsFromInput(args[0]))
                client.SendPacket(packet);
            
            caller.Output($"{(enable ? "Enabled" : "Disabled")} devmode for {CommandUtility.GetClientsStringFromInput(args[0])}");
        }
    }
}