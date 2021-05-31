using PlayerTrade.Net.Packets;
using RimWorld;
using Verse;

namespace PlayerTrade
{
    public static class AnnouncementUtility
    {
        public static void Show(PacketAnnouncement packet)
        {
            if (packet.Type == PacketAnnouncement.MessageType.Message)
            {
                Messages.Message(packet.Message, MessageTypeDefOf.NeutralEvent, false);
            }
            else if (packet.Type == PacketAnnouncement.MessageType.Dialog)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(packet.Message, "Close"));
            }
        }
    }
}
