using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
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
