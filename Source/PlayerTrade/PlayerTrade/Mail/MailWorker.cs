using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerTrade.Net;
using PlayerTrade.Patches;
using Verse;
using Verse.Sound;

namespace PlayerTrade.Mail
{
    public class MailWorker
    {
        public Client Client;

        public MailWorker(Client client)
        {
            Client = client;

            Client.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (e.Id == Packet.MailPacketId)
            {
                // Mail packet received
                PresentMail((PacketMail) e.Packet);
            }
        }

        public static void PresentMail(PacketMail mail)
        {
            // Receive letter
            StandardLetter letter = (StandardLetter) LetterMaker.MakeLetter(DefDatabase<LetterDef>.GetNamed("PlayerMail"));
            letter.label = $"{mail.Title} - {RimLinkComp.Find().Client.GetName(mail.From)}";
            letter.title = mail.Title;
            letter.text = mail.Body;
            Find.LetterStack.ReceiveLetter(letter);

            if (mail.SoundDefName != null)
            {
                // Play sound
                Log.Message("Playing mail sound: " + mail.SoundDefName);
                Patch_CameraDriver_Update.PendingOneshots.Add(DefDatabase<SoundDef>.GetNamed(mail.SoundDefName));
            }
        }
    }
}
