using System.Text;
using System.Threading.Tasks;
using RimLink.Net;

namespace TradeServer.Commands
{
    /// <summary>
    /// Outputs a dump of the packet ID to packet type mapping.
    /// </summary>
    public class CommandPacketIds : Command
    {
        public override string Name => "packetids";
        public override string Usage => "";
        public override async Task Execute(Caller caller, string[] args)
        {
            var sb = new StringBuilder("Server Packet ID Mapping\n");
            foreach (var mapping in Packet.Packets)
                sb.AppendLine($"{mapping.Key} = {mapping.Value.FullName}");
            caller.Output(sb.ToString());
        }
    }
}
