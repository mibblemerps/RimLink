using RimLink.Net.Packets;
using RimWorld;
using Verse;

namespace RimLink.Util
{
    public static class AnnouncementUtil
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
