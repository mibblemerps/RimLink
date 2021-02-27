using PlayerTrade.Net;
using PlayerTrade.Net.Packets;
using PlayerTrade.Patches;
using Verse;

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
            if (e.Packet is PacketMail mailPacket)
            {
                // Mail packet received
                PresentMail(mailPacket);
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
